#ifndef RAYTRACING_INCLUDED
#define RAYTRACING_INCLUDED

struct Ray {
	float3 origin;
	float3 direction;
};

struct HitInfo {
	float3 position;
	float3 normal;
	float3 color;
	float distance;
};

struct Sphere {
	float3 position;
	float3 color;
	float radius;
};

Ray CreateRay(float3 origin, float3 direction) {
	Ray ray;
	ray.origin = origin;
	ray.direction = direction;
	return ray;
}

HitInfo CreateHitInfo() {
	HitInfo hitInfo;
	hitInfo.position = float3(0, 0, 0);
	hitInfo.normal = float3(0, 0, 0);
	hitInfo.distance = DISTANCE_INFINITY;
	hitInfo.color = float3(0, 0, 0);
	return hitInfo;
}

Sphere CreateSphere(float3 position, float3 color, float radius) {
	Sphere sphere;
	sphere.position = position;
	sphere.color = color;
	sphere.radius = radius;
	return sphere;
}

#endif