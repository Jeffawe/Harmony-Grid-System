# Harmony Grid System

This is a Unity-based 3D grid system that allows users to create a customizable grid and place objects within it. The system supports both runtime grid placement and editor-based object placement, with a helpful converter script to transform meshes into the proper prefabs for easy placement on the grid.

## Features

- **Customizable 3D Grid**: Create grids of various sizes and cell dimensions.
- **Mesh to Prefab Converter**: A script to convert meshes into prefabs suitable for grid placement.
- **Editor Support**: Place objects in the grid directly in the Unity Editor for easier testing and design.
- **Grid Object Placement**: Easily place objects on the grid with snap functionality for better alignment.
  
## Installation

1. Download or clone this repository to your local machine.
2. Open the project in Unity.
3. The system is set up to work directly within the Unity Editor, with all the necessary scripts and prefabs included.
4. Ensure that the **Editor** folder is set up in your project for editor-only scripts.

## Usage

### Setting Up the Grid

1. **Create a Grid**: You can create a grid in the Unity scene by dragging the `GridController` prefab into the scene. This will create a default grid of configurable size.
   
2. **Grid Settings**: In the Inspector, you can modify grid settings such as the number of rows, columns, and the size of each cell.

3. **Object Placement**: You can place objects on the grid by using a helpful Editor placing script, using scripts to automate object placement during runtime or placing them using the mouse in runtime.

### Mesh to Prefab Conversion

1. **Prepare Mesh**: Import a 3D mesh into your Unity project.
2. **Convert the Mesh**: Go to Tools > Harmony Grid System > Convert Objects. Fill in the required setting or leave as default and the converter script will take the mesh and convert it into a prefab that is ready to be placed on the grid.
3. **Prefab Placement**: Once converted, the prefab can be placed on the grid just like any other object.


## Example Workflow

1. **Create Grid**: Drag the `GridController` into your scene to generate a new grid.
2. **Prepare Mesh**: Import any mesh you want to use.
3. **Convert to Prefab**: Use the mesh converter to turn your mesh into a prefab.
4. **Place Prefabs**: Drag the newly created prefab into the scene and place it onto the grid.

## Customization

You can customize the grid's size, shape, and placement functionality by modifying the `GridManager` and other related scripts. The system is flexible and allows for easy integration into different game types or scenes.

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by grid-based game mechanics.
- Thanks to the Unity community for their continuous support and tutorials.
- Big thanks to [CodeMonkey](https://www.codemonkey.com) and his amazing video series [Grid System in Unity](https://www.youtube.com/watch?v=waEsGu--9P8&list=PLzDRvYVwl53uhO8yhqxcyjDImRjO9W722) of which this takes heavy inspiration from.

