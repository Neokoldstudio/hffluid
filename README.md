# hffluid

This projects runs on the version 2022.3.46f1 of the Unity game engine and uses compute shaders to handle the heavy computations behind the simulation.

To have a look at the code behind the simulation, find the compute shader under Assets/SWE/ShallowWater.compute

The compute shader is executed by the script "WaterSimulation.cs" which can be found the the same folder as the compute shader.

controls of the simulation are the following : 
- Space : start/stop the simulation
- R : Restart the simulation
- Right Arrow : advance the simulation manually
- N : advance the simulation by one step
