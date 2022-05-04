#pragma once

#include <cstdint>
#include <box2d/b2_math.h>

#ifdef _WIN32
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C"
#endif

struct MoveUnit
{
    float radius;
    b2Vec2 position;
    b2Vec2 velocity;
    b2Vec2 force;
};

struct MoveContact
{
    int32_t a;
    int32_t b;
};

struct MoveWorld;

EXPORT_API MoveWorld* move_new();
EXPORT_API void move_delete(MoveWorld* world);
EXPORT_API void move_step(MoveWorld* world, float dt, void* units, int32_t length, int32_t sizeInBytes);
EXPORT_API void move_add_obstacle(MoveWorld* world, b2Vec2* vertices, int32_t length);
EXPORT_API int32_t move_get_contacts(MoveWorld* world, MoveContact* contacts, int32_t length);


struct NavMeshPolygon;

EXPORT_API NavMeshPolygon* navmesh_polygon_new(int32_t* polylines, int32_t length, b2Vec2* vertices);
EXPORT_API void navmesh_polygon_delete(NavMeshPolygon* polygon);
EXPORT_API int32_t navmesh_polygon_triangulate(NavMeshPolygon* polygon, uint16_t** indices);
