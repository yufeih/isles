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

EXPORT_API void move_step(MoveWorld* world, float dt, void* units, int32_t length, int32_t sizeInBytes);
EXPORT_API void move_add_obstacle(MoveWorld* world, b2Vec2* vertices, int32_t length);

EXPORT_API int32_t move_get_contacts(MoveWorld* world, MoveContact* contacts, int32_t length);
