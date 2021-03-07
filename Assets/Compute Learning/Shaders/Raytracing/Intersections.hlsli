#ifndef INTERSECTIONS_INCLUDED
#define INTERSECTIONS_INCLUDED

void IntersectGroundPlane(Ray ray, inout HitInfo bestHit, float planeY) {
	float t = (-ray.origin.y+planeY) / ray.direction.y;
	if(t > 0 && t < bestHit.distance) {
		bestHit.distance = t;
		bestHit.position = ray.origin + t * ray.direction;
		bestHit.normal = float3(0, 1, 0);
		bestHit.color = float3(1, 1, 1);
	}
}

void IntersectSphere(Ray ray, inout HitInfo bestHit, Sphere sphere) {
	float3 d = ray.origin - sphere.position;
	float p1 = -dot(ray.direction, d);
	float p2sqr = p1 * p1 - dot(d,d) + sphere.radius * sphere.radius;

	if(p2sqr < 0) return;
	float p2 = sqrt(p2sqr);
	float t = p1 - p2 > 0 ? p1 - p2 : p1 + p2;
	if(t > 0 && t < bestHit.distance) {
		bestHit.distance = t;
		bestHit.position = ray.origin + t * ray.direction;
		bestHit.normal = normalize(bestHit.position - sphere.position);
		bestHit.color = sphere.color;
	}
}

#endif