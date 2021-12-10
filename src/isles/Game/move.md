state: `idle` | `move`

a | b | behavior
--|--|--
idle | idle | both idle
move | idle | _b_: give way to _a_, than back, limit speed <br/> _a_: full speed ahead
move | move | 

contact:
- move along contact manifold normal
- randomize on stuck
- give up on timeout
