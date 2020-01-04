Shader "Unlit/Grass"
{
	Properties
	{
		_FlowerTex("Flower Texture", 2D) = "white" {}
		_FlowerAlpha("Flower Alpha", 2D) = "white" {}
		_BaseHeight("Height of Flower", Range(0, 3)) = 0.001
		_BaseThickness("Thickness of Flower", Range(0, 0.1)) = 0.001
		_FlowerSize("Flower Size", Range(0, 1)) = 0.5
		_WindScale("Wind Scale", Range(0, 0.5)) = 0.5
		_WindField("Wind Field", 2D) = "black" {}
	}
		SubShader
		{
			Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
			LOD 100
			Pass
			{

				Tags{ "LightMode" = "ForwardBase" }
				AlphaToMask On
				Cull Off

				CGPROGRAM
				#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				struct g2f
				{
					float2 uv : TEXCOORD0;
					float4 pos : SV_POSITION;
					float3 normal : NORMAL;
					float2 baseUv : TEXCOORD1;
				};

				struct v2g
				{
					float4 pos    : POSITION;
					float3 normal : NORMAL;
					float2 uv     : TEXCOORD0;
				};

				sampler2D _RandomMap;
				sampler2D _FlowerTex;
				sampler2D _FlowerAlpha;
				sampler2D _WindField;
				float _BaseHeight;
				float _BaseThickness;
				float _FlowerSize;
				float _WindScale;

				v2g vert(appdata_base v)
				{
					v2g o = (v2g)0;
					o.pos = mul(unity_ObjectToWorld, v.vertex);
					o.normal = float3(0,0,0);
					o.uv = v.texcoord;
					return o;
				}

				g2f Zero() {
					g2f o;
					o.pos = (float4)0;
					o.uv = (float2)0;
					o.normal = (float3)0;
					return o;
				}

				float3 GetPoint(float3 start, float3 end, float x)
				{
					float w = x * x * (3 - x) * 0.5;
					return start + (end - start) * w;
				}

				void MakeList(int vertexCount, float2 uv, float3 root, float3 right, float3 look, inout g2f v[18])
				{
					float2 wind = tex2Dlod(_WindField, float4(uv, 0, 0)).xy * 10;
					wind += float2(_CosTime.z + frac(root.x), _SinTime.z + frac(root.y));;
					float3 up = float3(0, 1, 0);
					float currentV = 1.f;
					float offsetV = 1.f / ((vertexCount / 2) - 1);
					//根据风向和风力，计算顶点弯曲到哪
					float2 windDir = normalize(wind);
					float windForce = clamp(length(wind) * UNITY_HALF_PI * _WindScale, 0, 1.5);
					float sinW = sin(windForce);
					float cosW = cos(windForce);
					//根据悬臂梁公式计算插值后的网格顶点
					for (int i = 6; i < 6 + vertexCount; i++)
					{
						float3 target = root + float3(windDir.x, 0, windDir.y) * 0.75 * sinW * _BaseHeight * currentV + up * _BaseHeight * cosW * currentV;
						float3 start = root + up * _BaseHeight * currentV;
						float3 temp;
						if (fmod(i, 2) == 0)
						{
							v[i].pos = float4(GetPoint(start, target, currentV) + right * 0.5 * _BaseThickness, 1);
							v[i].uv = float2(0.125, i / (vertexCount * 0.5f - 1));
							v[i].normal = look;
							temp = v[i].pos - v[i - 2].pos;
							temp = cross(temp, look);
							if (i == 6)
								v[i].pos = float4(GetPoint(start, target, currentV) + normalize(temp) * 0.5 * _BaseThickness, 1);
							else
								v[i].pos = float4(GetPoint(start, target, currentV) - normalize(temp) * 0.5 * _BaseThickness, 1);
						}
						else
						{
							v[i].pos = float4(GetPoint(start, target, currentV) - right * 0.5 * _BaseThickness, 1);
							v[i].uv = float2(0, i / (vertexCount * 0.5f - 1));
							v[i].normal = look;
							temp = v[i].pos - v[i - 2].pos;
							temp = cross(temp, look);
							if (i == 7)
								v[i].pos = float4(GetPoint(start, target, currentV) - normalize(temp) * 0.5 * _BaseThickness, 1);
							else
								v[i].pos = float4(GetPoint(start, target, currentV) + normalize(temp) * 0.5 * _BaseThickness, 1);
							currentV -= offsetV;
						}
					}
					//计算花盘的朝向
					float3 tangent = normalize(float3(windDir.x, -windForce, windDir.y)) * _FlowerSize;
					//计算出风所在的面的法向量
					float3 windNormal = normalize(cross(float3(windDir.x, 0, windDir.y), float3(windDir.x, 1, windDir.y))) * _FlowerSize;
					//挤出花盘，这里就不用走摄像机算出来的单位向量了，用自己算的量
					float3 center = root + float3(windDir.x, 0, windDir.y) * 0.75 * sin(windForce) * _BaseHeight + up * _BaseHeight * cos(windForce);
					v[0].pos = float4(center - tangent - windNormal, 1);
					v[0].normal = float3(0,1,0);
					v[0].uv = float2(0.125, 0.125);

					v[1].pos = float4(center - tangent + windNormal, 1);
					v[1].normal = float3(0,1,0);
					v[1].uv = float2(0.125, 1);

					v[2].pos = float4(center + tangent - windNormal, 1);
					v[2].normal = float3(0,1,0);
					v[2].uv = float2(1, 0.125);

					v[3].pos = float4(center + tangent + windNormal, 1);
					v[3].normal = float3(0,1,0);
					v[3].uv = float2(1, 1);

					v[4].pos = float4(center, 1);
					v[4].normal = float3(0,1,0);
					v[4].uv = float2(0.5625, 0.5625);

					v[5].pos = float4(center, 1);
					v[5].normal = float3(0,1,0);
					v[5].uv = float2(0.5625, 0.5625);
				}
				
				//输入的是点，输出的是三角形
				[maxvertexcount(18)]
				void geom(point v2g p[1], inout TriangleStream<g2f> triStream)
				{
					//定义【世界坐标系】下的向上的单位向量
					float3 up = float3(0,1,0);
					//拿到摄像机的观察向量
					float3 look = _WorldSpaceCameraPos - p[0].pos;
					//计算距离，方便LOD
					float lookDist = length(look);
					//因为花的根茎仅在XZ平面上各向观察有厚度，所以Y轴解锁
					look.y = 0;
					look = normalize(look);
					//叉乘拿到【世界坐标系】下，从摄像机方向看的向右的单位向量
					float3 right = cross(up, look);
					//构造面，顶点之所以是vector4是为了矩阵运算
					g2f v[18];
					for (int initIdx = 0; initIdx < 18; initIdx++) {
						v[initIdx] = Zero();
						v[initIdx].baseUv = p[0].uv;
					}
					//LOD
					if (lookDist > 100)
					{
						MakeList(4, p[0].uv, p[0].pos, right, look, v);
						//将世界坐标系下的几个点转到屏幕空间，然后传入到流中
						for (int outIdx = 0; outIdx < 6 + 4; outIdx++)
						{
							v[outIdx].pos = UnityObjectToClipPos(v[outIdx].pos);
							triStream.Append(v[outIdx]);
						}
					}
					else if (lookDist > 30)
					{
						MakeList(8, p[0].uv, p[0].pos, right, look, v);
						//将世界坐标系下的几个点转到屏幕空间，然后传入到流中
						for (int outIdx = 0; outIdx < 6 + 8; outIdx++)
						{
							v[outIdx].pos = UnityObjectToClipPos(v[outIdx].pos);
							triStream.Append(v[outIdx]);
						}
					}
					else
					{
						MakeList(12, p[0].uv, p[0].pos, right, look, v);
						//将世界坐标系下的几个点转到屏幕空间，然后传入到流中
						for (int outIdx = 0; outIdx < 6 + 12; outIdx++)
						{
							v[outIdx].pos = UnityObjectToClipPos(v[outIdx].pos);
							triStream.Append(v[outIdx]);
						}
					}
				}

				fixed4 frag(g2f i) : SV_Target
				{
					//计算法线
					half3 worldNormal = UnityObjectToWorldNormal(i.normal);
					//获取环境光，此处用的球谐函数
					fixed3 ambient = ShadeSH9(half4(worldNormal, 1));
					//半兰伯特的算法，黑暗处更明亮
					fixed3 diffuseLight = (dot(worldNormal, UnityWorldSpaceLightDir(i.pos)) * 0.5 + 0.5) * _LightColor0;

					//Blinn-Phong模型的高光
					//fixed3 halfVector = normalize(UnityWorldSpaceLightDir(i.pos) + WorldSpaceViewDir(i.pos));
					//fixed3 specularLight = pow(saturate(dot(worldNormal, halfVector)), 15) * _LightColor0;

					fixed3 light = ambient + diffuseLight;

					// 观察速度场
					//return tex2D(_WindField, i.baseUv);

					return fixed4(tex2D(_FlowerTex, i.uv).rgb * light, tex2D(_FlowerAlpha, i.uv).r);
				}
				ENDCG
			}
		}
}