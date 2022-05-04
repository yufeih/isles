#pragma once

#include "api.h"
#include <box2d/b2_math.h>

struct NavMeshPolygon;

EXPORT_API NavMeshPolygon* navmesh_new_polygon();
EXPORT_API void navmesh_delete_polygon(NavMeshPolygon* polygon);
EXPORT_API void navmesh_polygon_add_polylines(NavMeshPolygon* polygon, b2Vec2* vertices, int length);
EXPORT_API int32_t navmesh_polygon_triangulate(NavMeshPolygon* polygon, uint16_t** indices);
