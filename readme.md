# Easy AI

**Easily create basic artificial intelligence in Unity.**

- [Overview](#overview "Overview")
- [Features](#features "Features")
- [Installation](#installation "Installation")
  - [Package Manager](#package-manager "Package Manager")
  - [Manually](#manually "Manually")
- [Script Templates](#script-templates "Script Templates")
- [Getting Started](#getting-started "Getting Started")
- [Dependencies](#dependencies "Dependencies")

# Overview

Easy AI was created to allow for easily creating intelligent agents in Unity without a need for a deep understanding of Unity itself. Easy AI handles all boilerplate such as agent movement, cameras, and a GUI rendering automatically so you can focus on developing the behaviour of your agents and let Easy AI take care of the rest. This library is mainly intended for teaching and learning purposes that emphasizes a distinct separation between agents and their sensors, actuators, and minds, as well as the percepts and actions which allow these components to communicate. In a production environment, you would instead most likely want to create specifically optimized agents that directly have access to their required information abd thus Easy AI is not recommended for use directly in a production environment but its core logic can still be used as a starting point for specialized agents.

# Features

- Three types of agents allowing for movement either by directly moving the transform, using a character controller, or using a rigidbody.
- Agents will automatically collect all sensors, actuators, and minds attached to them meaning you do not need to worry about forgetting to assign any references in the inspector.
- An agent manager handles all agents which allows for limiting how many agents are updated during a single frame meaning you can use Easy AI even if you have a lot of agents in the scene or your computer is older.
- A built in messaging system which allows for displaying messages on the GUI for ease of seeing exactly what your agents are performing at any given moment.
- The built in GUI allows for selecting every agent, sensor, and actuator in the scene so you can see their exact details and messages.
- Several included cameras.
  - A camera type to look at the currently selected agent from a fixed position.
  - A camera type to follow the currently selected agent from behind and zoom in and out.
  - A camera type to track the currently selected agent from above and zoom in and out.
  - All cameras are selectable from the built in GUI which handles ensuring they are all focused on the currently selected agent.
- Easily implement methods for drawing lines directly to the screen so you can further visualize what your agents are doing.
- The built in GUI includes controls to easily play, pause, and step through a single time step.
- Fully documented source code under the MIT licence.

# Installation

You can install Easy AI either through the package manager or by manually downloading it as a ZIP and adding it to your project, with the package manager being the recommended mode of installation.

## Package Manager

*Note this method requires you have GIT installed on your computer.*

1. Click the "Code" button above and under "Clone", copy the https URL.
2. Open your Unity project and go to "Window > Package Manager" and hit the "+" icon in the top left of the Package Manager window followed by "Add package from git URL..." and paste in the link you copied in step one.

## Manually

1. Click the "Code" button above and click "Download ZIP".
2. Extract the ZIP file.
3. Open your Unity project and go to "Window > Package Manager" and hit the "+" icon in the top left of the Package Manager window followed by "Add package from disk..." and navigate to the extracted package where you must select the "package.json" file.

# Getting Started

- For a sample scene, in the project explorer go to "Packages > Easy AI > Samples > Easy AI Sample Scene". This scene demonstrates the three included types of agents and how they move compared to each other and has a few sample scripts all of which are fully documented in the same directory.
- The general workflow for Easy AI is as follows:
  - Sensors generate percepts which are sent to the agent.
  - Percepts are passed to the mind of the agent of processing where it decides on actions to take.
  - Actions are passed to the agent's actuators where they will perform tasks.
- There must be exactly one "AgentManager" or a component deriving from it present in every scene.
- To create a starter agent, right click in the hierarchy and go to "Easy AI > Agents" followed by the type of agent you wish to create.
  - Add your sensors and percepts to either this agent or any of its child objects where they will automatically be linked when the application is run.
  - Cameras can be added in the same way with "Easy AI > Cameras" in the hierarchy.

# Script Templates

Although you can simply create a new script in Unity and change it to inherit from sensor, actuator, mind, or performance measure, you can add a few more files to allow for you to right click in the project explorer and go to "Create > Easy AI" followed by the type of script you used to create. These need to be added manually outside of the package to work. To install these script templates:
1. Go [here](https://github.com/StevenRice99/Easy-AI-Script-Templates "Easy AI Script Templates").
2. Click the "Code" button above and click "Download ZIP".
3. Extract the ZIP file.
4. Copy "ScriptTemplates" and "ScriptTemplates.meta" directly into the "Assets" folder of your Unity project.
5. If Unity is running, restart it and the script templates will be working.

# Dependencies

- Although there are no known version-specific requirements, this library has been developed on Unity 2020.3 and thus is it recommended to use this version to avoid any potential issues.
- Easy AI requires Unity's [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/manual/index.html "Input System") for zooming in and out with the included cameras.