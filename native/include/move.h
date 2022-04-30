#pragma once

#include "api.h"
#include <box2d/box2d.h>

struct MoveUnit
{
    float radius;
    b2Vec2 position;
    b2Vec2 velocity;
    b2Vec2 force;
};

struct MoveWorld;

EXPORT_API MoveWorld* move_new();
EXPORT_API void move_delete(MoveWorld* world);
EXPORT_API void move_step(MoveWorld* world, void* units, int unitsLength, int unitSizeInBytes, float dt);
EXPORT_API int move_query_aabb(MoveWorld* world, b2AABB* aabb, int* units, int unitsLength);
EXPORT_API int move_raycast(MoveWorld* world, b2Vec2* a, b2Vec2* b, int* unit);
