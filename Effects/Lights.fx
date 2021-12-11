sampler uImage0 : register(s0);

static const float SHADOW_CUTOFF_ALPHA = 0.5f;
static const float Pi2 = 6.28318530718f;

//Technique adapted from: http://www.catalinzima.com/2010/07/my-technique-for-the-shader-based-dynamic-2d-shadows/

uniform float2 lightCenter;
uniform float2 sizeMult;
uniform float2 sizeBlock;
float4 DistanceToShadowcaster(float2 coords : TEXCOORD0) : COLOR0
{
	float c = tex2D(uImage0,coords).a;
	float4 outCol = float4(1,1,1,1);

	float2 dist = (coords - lightCenter) * sizeMult;

	float2 distC = abs(dist);
	if (c > SHADOW_CUTOFF_ALPHA && !(distC.x <= sizeBlock.x && distC.y <= sizeBlock.y)) {
		outCol.xyz = (length(dist)).xxx;
	}

	return outCol;
}

float4 DistortEquidistantAngle(float2 coords : TEXCOORD0) : COLOR0
{
	float u0 = coords.x * 2 - 1;
	float v0 = coords.y * 2 - 1;

	v0 = v0 * abs(u0);
	v0 = (v0 + 1) / 2;
	float2 newCoords = float2(coords.x, v0);

	float horizontal = tex2D(uImage0, newCoords).r;
	float vertical = tex2D(uImage0, newCoords.yx).r;
	return float4(horizontal,vertical ,0,1);
}


uniform float texWidth;
float4 HorizontalReduce(float2 coords  : TEXCOORD0) : COLOR0
{
	float2 color = tex2D(uImage0, coords * float2(2, 1) + float2(-texWidth/2, 0)).xy;
	float2 colorR = tex2D(uImage0, coords * float2(2, 1) + float2(texWidth/2, 0)).xy;
	float2 result = float2(color.x < colorR.x ? color.x : colorR.x, color.y < colorR.y ? color.y : colorR.y);
	return float4(result,0,1);
}


uniform float3 lightColor;
uniform texture shadowMapTexture;
sampler reducedShadowMap = sampler_state
{
	Texture = <shadowMapTexture>;
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};
float GetShadowDistanceH(float2 coords)
{
	float u = coords.x;
	float v = coords.y;

	u = abs(u - 0.5f) * 2;
	v = v * 2 - 1;
	float v0 = v / u;
	v0 = (v0 + 1) / 2;

	float2 newCoords = float2(coords.x, v0);
	return tex2D(reducedShadowMap, newCoords).r;
}
float GetShadowDistanceV(float2 coords)
{
	float u = coords.y;
	float v = coords.x;

	u = abs(u - 0.5f) * 2;
	v = v * 2 - 1;
	float v0 = v / u;
	v0 = (v0 + 1) / 2;

	float2 newCoords = float2(coords.y, v0);
	return tex2D(reducedShadowMap, newCoords).g;
}
float4 ApplyShadow(float2 coords: TEXCOORD0) : COLOR0
{
	float distance = length(coords - 0.5f);

	float shadowMapDistance;
	float nY = 2.0f * (coords.y - 0.5f);
	float nX = 2.0f * (coords.x - 0.5f);

	if (abs(nY) < abs(nX))
	{
		shadowMapDistance = GetShadowDistanceH(coords);
	}
	else
	{
		shadowMapDistance = GetShadowDistanceV(coords);
	}

	float light = step(distance, shadowMapDistance * 0.5f) * (1.0f - (distance * 2.0f));

	return float4(lightColor * light, light);
}


uniform texture lightMapTexture;
sampler lightMap = sampler_state
{
	Texture = <lightMapTexture>;
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};
uniform float darkBrightness; // 0 - 1
uniform float brightBrightness; // 1 - Inf (10-ish?)
uniform float brightnessGrowthBase; // 1 - Inf (5-ish?)
uniform float brightnessGrowthRate; // 0 - Inf (10-ish?)
uniform float2 blurDistance; //0 - 2 px-ish?
float4 CompositeFinal(float2 coords : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(uImage0, coords);
	const float4 light = tex2D(lightMap, coords);

	float3 finalLight = light.xyz;

	for (float d = 0.0; d < Pi2; d += Pi2 / 6)
	{
		for (float i = 0.3333; i <= 1.0; i += 0.3333)
		{
			finalLight += tex2D(lightMap, coords + float2(cos(d), sin(d)) * (1 - light.a) * blurDistance * i).xyz;
		}
	}

	finalLight /= 3 * 6;

	color.xyz *= (brightBrightness - darkBrightness).xxx * (1 - pow(brightnessGrowthBase.xxx, -(finalLight * brightnessGrowthRate.xxx))) + darkBrightness.xxx;

	return color;
}


technique Shadowcasting
{
	pass DistanceToShadowcaster
	{
		PixelShader = compile ps_2_0 DistanceToShadowcaster();
	}
	pass DistortEquidistantAngle
	{
		PixelShader = compile ps_2_0 DistortEquidistantAngle();
	}
	pass HorizontalReduce
	{
		PixelShader = compile ps_2_0 HorizontalReduce();
	}
	pass ApplyShadow
	{
		PixelShader = compile ps_2_0 ApplyShadow();
	}
	pass CompositeFinal
	{
		PixelShader = compile ps_2_0 CompositeFinal();
	}
}