#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

float rand( float2 coord ){
	return frac(sin(dot(coord.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float3 rand3( float2 coord ){
	float t = sin(coord.x + coord.y * 1e3);
	return float3(frac(t*1e4), frac(t*1e6), frac(t*1e5));
}

#endif