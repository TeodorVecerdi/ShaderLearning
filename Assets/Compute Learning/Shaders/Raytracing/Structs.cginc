#ifndef STRUCTS_INCLUDED
#define STRUCTS_INCLUDED

struct Ray {
	float3 origin;
	float3 direction;
	float3 energy;
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
	ray.energy = float3(1, 1, 1);
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

Ray CreateRayFromCamera(float2 uv) {
	// transform camera to world space
	float3 origin = mul(CameraToWorld, float4(0,0,0,1)).xyz;

	// Invert the perspective projection
	float3 direction = mul(CameraInverseProjection, float4(uv, 0, 1)).xyz;
	// Transform the direction from camera space to world space and normalize
	direction = mul(CameraToWorld, float4(direction, 0.0)).xyz;
	direction = normalize(direction);
	
	return CreateRay(origin, direction);
}


#endif