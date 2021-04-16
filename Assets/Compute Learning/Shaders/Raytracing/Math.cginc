#ifndef RAYTRACING_MATH_INCLUDED
#define RAYTRACING_MATH_INCLUDED

#include "../Utils/Random.cginc"

float3x3 GetTangentSpace(const float3 normal) {
	// Choose a helper vector for the cross product
	float3 helper = float3(1, 0, 0);
	if (abs(normal.x) > 0.99f)
		helper = float3(0, 0, 1);

	// Generate vectors
	const float3 tangent = normalize(cross(normal, helper));
	const float3 binormal = normalize(cross(normal, tangent));
	return float3x3(tangent, binormal, normal);
}

float3 SampleHemisphere(const float3 normal) {
	// Uniformly sample hemisphere direction
	float cosTheta = rand();
	float sinTheta = sqrt(max(0.0f, 1.0f - cosTheta * cosTheta));
	float phi = 2 * PI * rand();
	float3 tangentSpaceDir = float3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
	// Transform direction to world space
	return mul(tangentSpaceDir, GetTangentSpace(normal));
}



float sdot(const float3 a, const float3 b, const float c = 1.0f) {
	return saturate(dot(a, b) * c);
}


#endif
