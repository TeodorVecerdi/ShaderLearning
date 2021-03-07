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
	float distance;

	float3 albedo;
	float3 specular;
};

struct Sphere {
	float3 position;
	float radius;
	uint materialIndex;
};

struct Plane {
	float y;
	uint materialIndex;
};

struct Material {
	float3 albedo;
	float3 specular;
};

static Ray CreateRay(const float3 origin, const float3 direction) {
	Ray ray;
	ray.origin = origin;
	ray.direction = direction;
	ray.energy = float3(1, 1, 1);
	return ray;
}

static HitInfo CreateHitInfo() {
	HitInfo hitInfo;
	hitInfo.position = float3(0, 0, 0);
	hitInfo.normal = float3(0, 0, 0);
	hitInfo.distance = DISTANCE_INFINITY;
	hitInfo.albedo = float3(0, 0, 0);
	hitInfo.specular = float3(0, 0, 0);
	return hitInfo;
}

Sphere CreateSphere(const float3 position, const float radius, const uint materialIndex) {
	Sphere sphere;
	sphere.position = position;
	sphere.radius = radius;
	sphere.materialIndex = materialIndex;
	return sphere;
}

Plane CreatePlane(const float y, const uint materialIndex) {
	Plane plane;
	plane.y = y;
	plane.materialIndex = materialIndex;
	return plane;
}

Material CreateMaterial(const float3 albedo, const float3 specular) {
	Material material;
	material.albedo = albedo;
	material.specular = specular;
	return material;
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