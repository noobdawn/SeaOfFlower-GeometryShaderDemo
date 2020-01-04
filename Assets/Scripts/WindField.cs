using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindField : MonoBehaviour
{
    enum Size
    {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024
    }

    static class RT
    {
        public static RenderTexture u1, u2, u3;
        public static RenderTexture p1, p2;
        public static void Init(int width, int height)
        {
            u1 = CreateTexture(2, width, height);
            u2 = CreateTexture(2, width, height);
            u3 = CreateTexture(2, width, height);
            p1 = CreateTexture(1, width, height);
            p2 = CreateTexture(1, width, height);
        }

        public static RenderTexture CreateTexture(int component, int width, int height)
        {
            var format = RenderTextureFormat.ARGBHalf;
            if (component == 2) format = RenderTextureFormat.RGHalf;
            if (component == 1) format = RenderTextureFormat.RHalf;

            var result = new RenderTexture(width, height, 0, format);
            result.enableRandomWrite = true;
            result.Create();
            return result;
        }

        public static void Dispose()
        {
            Destroy(u1);
            Destroy(u2);
            Destroy(u3);
            Destroy(p1);
            Destroy(p2);
        }
    }

    static class Kernel
    {
        public static int Advect = 0;
        public static int Jacobi1 = 1;
        public static int Jacobi2 = 2;
        public static int Force = 3;
        public static int Divergence = 4;
        public static int Subtract = 5;
    }

    #region Open As Windfield
    public RenderTexture _windTex {get{return RT.u1;}}
    private bool _needUpdate = false;
    private Vector2 _pos, _dir;
    private float _windForce, _radius;
    
    public void AddWind(Vector2 pos, Vector2 dir, float force, float rad)
    {
        _needUpdate = true;
        _pos = pos;
        _dir = dir.normalized;
        _windForce = force;
        _radius = rad;
    }
    #endregion

    #region Property
    [SerializeField] Size _resolution = Size._512;
    [SerializeField] ComputeShader _compute;
    [SerializeField] float Viscosity = 1e-6f;
    int ResolutionX { get { return ThreadCountX << 3; } }
    int ThreadCountX { get { return ((int)_resolution) >> 3; } }
    #endregion

    #region Mono
    private void Awake()
    {
        RT.Init(ResolutionX, ResolutionX);
    }

    private void OnDestroy()
    {
        RT.Dispose();
    }

    float dt, dx;
    private void Update()
    {
        // common argument
        dt = Time.deltaTime;
        dx = 1f / ResolutionX;
        _compute.SetFloat("dx", dx);
        _compute.SetFloat("dt", dt);
        Advect();
        Diffuse();
        AddForce();
        ComputePressure();
        SubtractPressureGradient();
    }
    #endregion

    #region Procedure
    private void Advect()
    {
        _compute.SetTexture(Kernel.Advect, "U_in", RT.u1);
        _compute.SetTexture(Kernel.Advect, "U_out", RT.u2);
        _compute.Dispatch(Kernel.Advect, ThreadCountX, ThreadCountX, 1);
        // remember to swap rt
        var t = RT.u2;
        RT.u2 = RT.u1;
        RT.u1 = t;
    }

    private void Diffuse()
    {
        var alpha = dx * dx / (Viscosity * dt);
        var beta = alpha + 4;
        Graphics.CopyTexture(RT.u1, RT.u3);
        _compute.SetFloat("Alpha", alpha);
        _compute.SetFloat("Beta", beta);
        _compute.SetTexture(Kernel.Jacobi2, "B2_in", RT.u3);
        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernel.Jacobi2, "X2_in", RT.u1);
            _compute.SetTexture(Kernel.Jacobi2, "X2_out", RT.u2);
            _compute.Dispatch(Kernel.Jacobi2, ThreadCountX, ThreadCountX, 1);

            _compute.SetTexture(Kernel.Jacobi2, "X2_in", RT.u2);
            _compute.SetTexture(Kernel.Jacobi2, "X2_out", RT.u1);
            _compute.Dispatch(Kernel.Jacobi2, ThreadCountX, ThreadCountX, 1);
        }
    }
    
    private void AddForce()
    {
        Vector4 dir = Vector4.zero;
        Vector4 position = Vector4.zero;
        // curInput.x in [-0.5 * aspect, 0.5 * aspect]
        // curInput.y in [-0.5, 0.5]
        // 鼠标按下，计算力的方向
        if (_needUpdate)
        {
            // w is force strength
            position = _pos;
            dir = _dir;
            dir.w = _windForce;
            _needUpdate = false;
        }
        _compute.SetTexture(Kernel.Force, "U_in", RT.u1);
        _compute.SetTexture(Kernel.Force, "U_out", RT.u2);
        _compute.SetVector("ForceDirection", dir);
        _compute.SetVector("ForcePosition", position);
        _compute.SetFloat("ForceRadius", _radius);
        _compute.SetTexture(Kernel.Force, "U_in", RT.u1);
        _compute.SetTexture(Kernel.Force, "U_out", RT.u2);
        _compute.Dispatch(Kernel.Force, ThreadCountX, ThreadCountX, 1);
        // remember to swap rt
        var t = RT.u2;
        RT.u2 = RT.u1;
        RT.u1 = t;
    }

    private void ComputePressure()
    {
        // 计算速度场的散度，初始化压力场
        _compute.SetTexture(Kernel.Divergence, "U_in", RT.u1);
        _compute.SetTexture(Kernel.Divergence, "U_out", RT.u3);
        _compute.SetTexture(Kernel.Divergence, "P_out", RT.p1);
        _compute.Dispatch(Kernel.Divergence, ThreadCountX, ThreadCountX, 1);
    }

    private void SubtractPressureGradient()
    {
        // 将散度作为b传入计算，雅可比解泊松方程，求出压力场
        var alpha = -dx * dx;
        var beta = 4;
        _compute.SetFloat("Alpha", alpha);
        _compute.SetFloat("Beta", beta);
        _compute.SetTexture(Kernel.Jacobi1, "B1_in", RT.u3);
        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernel.Jacobi1, "X1_in", RT.p1);
            _compute.SetTexture(Kernel.Jacobi1, "X1_out", RT.p2);
            _compute.Dispatch(Kernel.Jacobi1, ThreadCountX, ThreadCountX, 1);

            _compute.SetTexture(Kernel.Jacobi1, "X1_in", RT.p2);
            _compute.SetTexture(Kernel.Jacobi1, "X1_out", RT.p1);
            _compute.Dispatch(Kernel.Jacobi1, ThreadCountX, ThreadCountX, 1);
        }
        // 速度场减压力场的梯度
        _compute.SetTexture(Kernel.Subtract, "U_in", RT.u1);
        _compute.SetTexture(Kernel.Subtract, "U_out", RT.u2);
        _compute.SetTexture(Kernel.Subtract, "P_in", RT.p1);
        _compute.Dispatch(Kernel.Subtract, ThreadCountX, ThreadCountX, 1);
        // remember to swap rt
        var t = RT.u2;
        RT.u2 = RT.u1;
        RT.u1 = t;
    }
    #endregion
}
