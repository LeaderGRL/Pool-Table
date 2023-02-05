# Pool Table Game

This is a (un)realistic pool table game made in Unity using C# and PhysX for physics simulation. The project aims to provide a realistic gaming experience, with accurate ball physics and sound effects.

## Features

- Unrealistic ball physics using PhysX
- Advanced ball spin and shot strength controls using mouse input
- Collision detection and sound effects for ball-to-ball and ball-to-table interactions
- Customizable ball and material properties

## Getting Started

To get started with this project, you'll need to have Unity installed on your computer. You can download Unity from the [official website](https://unity.com/).

### Prerequisites

- Unity (version 2021.1 or later)
- PhysX (included with Unity)

### Installing

1. Clone or download the repository to your local machine.
2. Open the project in Unity by selecting the `pool_table` folder.
3. In the Unity Editor, open the Scene named "MainScene".
4. Press the Play button to run the scene.

## Controls

- Left-click and drag the mouse to set the shot strength.
- Right-click and drag the mouse to set the spin on the ball. (not implemented yet)
- Release the mouse button to take the shot.

## Sound Effects

Sound effects are played when the balls collide with each other or the table. The sounds are triggered by the BallSound script, which is attached to each ball GameObject.

## Built With

- Unity - The game engine used
- C# - The programming language used
- PhysX - The physics simulation engine used

## Authors

- [Leader](https://github.com/LeaderGRL) - Initial work

## License

This project is licensed under the [MIT License](LICENSE.md).
