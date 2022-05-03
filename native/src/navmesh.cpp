#include <navmesh.h>
#include <box2d/b2_math.h>
#include <mapbox/earcut.hpp>

using N = uint16_t;

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

int navmesh_triangulate()
{
    std::vector<std::vector<b2Vec2>> polygon;

    // Fill polygon structure with actual data. Any winding order works.
    // The first polyline defines the main polygon.
    polygon.push_back({{100, 0}, {100, 100}, {0, 100}, {0, 0}});
    // Following polylines define holes.
    polygon.push_back({{75, 25}, {75, 75}, {25, 75}, {25, 25}});

    // Run tessellation
    // Returns array of indices that refer to the vertices of the input polygon.
    // e.g: the index 6 would refer to {25, 75} in this example.
    // Three subsequent indices form a triangle. Output triangles are clockwise.
    std::vector<N> indices = mapbox::earcut<N>(polygon);
    return 0;
}
