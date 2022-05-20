#pragma once

#include "api.h"
#include <box2d/box2d.h>

struct Movable
{
    float radius;
    b2Vec2 position;
    b2Vec2 velocity;
    b2Vec2 force;
    b2Body* body;
};

struct Obstacle
{
    float size;
    b2Vec2 position;
    b2Body* body;
};

struct MoveContact
{
    int32_t a;
    int32_t b;
};

EXPORT_API b2World* move_new();
EXPORT_API void move_delete(b2World* world);

EXPORT_API void move_step(
    b2World* world, float dt, Movable* movables, int32_t movablesLength,
    Obstacle* obstacles, int32_t obstaclesLength);

EXPORT_API int32_t move_get_next_contact(b2World* world, void** iterator, MoveContact* contact);
