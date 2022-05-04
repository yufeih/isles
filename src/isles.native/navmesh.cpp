#include "api.h"
#include <mapbox/earcut.hpp>

namespace mapbox {
namespace util {

template <>
struct nth<0, b2Vec2> {
    inline static auto get(const b2Vec2 &t) {
        return t.x;
    };
};
template <>
struct nth<1, b2Vec2> {
    inline static auto get(const b2Vec2 &t) {
        return t.y;
    };
};

} // namespace util
} // namespace mapbox

struct NavMeshPolygon
{
    std::vector<std::vector<b2Vec2>> polygon;
    std::vector<uint16_t> triangles;
};

NavMeshPolygon* navmesh_polygon_new(int32_t* polylines, int32_t polylinesLength, b2Vec2* vertices)
{
    auto polygon = new NavMeshPolygon();
    for (auto i = 0; i < polylinesLength; i++) {
        std::vector<b2Vec2> polyline;
        auto step = polylines[i];
        polyline.assign(vertices, vertices + step);
        polygon->polygon.push_back(std::move(polyline));
        vertices += step;
    }
    return polygon;
}

void navmesh_polygon_delete(NavMeshPolygon* polygon)
{
    delete polygon;
}

int32_t navmesh_polygon_triangulate(NavMeshPolygon* polygon, uint16_t** indices)
{
    polygon->triangles = mapbox::earcut<uint16_t>(polygon->polygon);
    *indices = polygon->triangles.data();
    return polygon->triangles.size();
}
