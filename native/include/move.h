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

struct MoveContact
{
    int32_t a;
    int32_t b;
};

struct MoveWorld;

EXPORT_API MoveWorld* move_new();
EXPORT_API void move_delete(MoveWorld* world);
EXPORT_API void move_step(MoveWorld* world, void* units, int32_t unitsLength, int32_t unitSizeInBytes, float dt);
EXPORT_API int32_t move_get_contacts(MoveWorld* world, MoveContact* contacts, int32_t contactsLength);
EXPORT_API int32_t move_query_aabb(MoveWorld* world, b2AABB* aabb, int32_t* units, int32_t unitsLength);
EXPORT_API int32_t move_raycast(MoveWorld* world, b2Vec2* a, b2Vec2* b, int32_t* unit);
