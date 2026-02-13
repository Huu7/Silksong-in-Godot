using Godot;
using System;

public partial class AnimationController : Node2D
{
	Hornet hornet;
	AnimatedSprite2D sprite;
	
	public override void _Ready()
	{	
		hornet = GetParent<Hornet>();
		sprite = GetParent().GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _Process(double delta)
	{
		if		(hornet.Velocity.X > 0) sprite.FlipH = false;
		else if (hornet.Velocity.X < 0) sprite.FlipH = true;

		string anim = "";
		if(hornet.IsOnFloor())anim = "Idle";

		if(hornet.IsOnFloor())
		{
			if	   (hornet.state.horizontal == HorizontalState.walk) anim = "Walk";
			else if(hornet.state.horizontal == HorizontalState.run) anim = "Run";
		}
		else
		{
			if     (hornet.state.vertical == VerticalState.fall) anim = "Fall";
			else if(hornet.state.vertical == VerticalState.groundJump) anim = "Jump";
			else if(hornet.state.vertical == VerticalState.airJump) anim = "MidAirJump";
		}

		sprite.Play(anim);
	}
}
