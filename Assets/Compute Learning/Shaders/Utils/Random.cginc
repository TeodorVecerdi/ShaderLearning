#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

float RANDOM__Seed = 0.0f;
float2 RANDOM__Pixel = 0.0f;

float rand(float2 coord) {
	return frac(sin(dot(coord.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float Random(float2 p) {
	const float2 K1 = float2(23.14069263277926, 2.665144142690225);
	return frac(cos(dot(p, K1)) * 43758.5453);
}

float Random2(float2 pixel) {
	const float result = frac(sin(RANDOM__Seed / 1000.0f + dot(pixel, float2(23.14069263277926, 2.665144142690225))) * 43758.5453f);
	RANDOM__Seed += 1.0f;
	return result;
}

float Random3() {
	return Random2(RANDOM__Pixel);
}

float rand() {
	float result = frac(sin(RANDOM__Seed / 100.0f + dot(RANDOM__Pixel, float2(12.9898f, 78.233f))) * 43758.5453f);
	RANDOM__Seed += 1.0f;
	return result;
}

#endif
