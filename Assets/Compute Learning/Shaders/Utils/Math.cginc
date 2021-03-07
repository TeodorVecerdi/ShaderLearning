#ifndef MATH_INCLUDED
#define MATH_INCLUDED

float SqrDistance(float2 a, float2 b) {
	return (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
}

float SqrDistance(float3 a, float3 b) {
	return (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + (b.z - a.z) * (b.z - a.z);
}

float SqrDistance(float4 a, float4 b) {
	return (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + (b.z - a.z) * (b.z - a.z) + (b.w - a.w) * (b.w - a.w);
}

float2 WorldToLocal(float2 local, float worldSize) {
	return local * worldSize;
}

float WorldToLocal(float local, float worldSize) {
	return local * worldSize;
}

float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget) {
	return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
}

#endif