using Godot;
using System;

public partial class Floor : CsgBox3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var material = new StandardMaterial3D();
		// Load the texture from the root project folder
		var texture = GD.Load<Texture2D>("res://smooth-plaster-wall.jpg");
		
		if (texture != null)
		{
			material.AlbedoTexture = texture;
			// Tile the texture 5x5 so it doesn't look overly stretched across a large floor
			material.Uv1Scale = new Vector3(5, 5, 5);
		}
		
		this.Material = material;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
