using Godot;
using System;

public class Permissions
{
	public bool canWalkRun = true;
}

public enum HorizontalState
{
	none, walk, run
}
public enum VerticalState
{
	none, groundJump, airJump, wallSliding, fall
}
public enum AbilityState
{
	none, wallJump, dash
}

public class PlayerState
{
	public HorizontalState horizontal {get; set;} = HorizontalState.none;
	public VerticalState vertical {get; set;} = VerticalState.none;
	public AbilityState ability {get; set;} = AbilityState.none;

	public bool IsJumping =>
		vertical is VerticalState.groundJump or VerticalState.airJump;
}
public class HasAbility
{
	public bool wallSlide = false;
	public bool faydownCloak = false;
}



public partial class Hornet : CharacterBody2D
{
	public PlayerState state = new();
	public Permissions permissions = new();
	public HasAbility hasAbility = new();
	
	public const float walkSpeed = 300.0f, runSpeed = 600.0f, JumpVelocity = -550.0f, maxJumpDuration = 0.25f, maxAirJumpDuration = 0.2f;
	public const int maxAirJumpCharge = 1;

	int airJumpCharge;
	float jumpDuration = 0;
	

	//Run Once
	public override void _Ready()
	{
		airJumpCharge = maxAirJumpCharge;
		hasAbility.wallSlide = true;
	}
	

	//Per Frame
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		if(!IsOnFloor()) velocity += GetGravity() * (float)delta;
		
		HandleJump(delta, ref velocity);
		if(hasAbility.wallSlide)
			HandleSliding();
		HandleMovement(ref velocity);
		// HandleMovementAbilities(ref velocity);
		
		Velocity = velocity;
		ShowDebug();
		MoveAndSlide();
		UpdateState();
	}


	void ShowDebug()
	{
		GD.Print(Velocity, "  ⌚:", jumpDuration, "  🚶‍♂️‍➡️:", permissions.canWalkRun,
			"\n", state.horizontal, "  ", state.vertical, "  ", state.ability, "  🔋:", airJumpCharge);
	}
	void HandleSliding()
	{
		if(IsOnWall() && !IsOnFloor())
		{
			state.vertical = VerticalState.wallSliding;
			permissions.canWalkRun = false;
		}
		else
		{
			permissions.canWalkRun = true;
		}
	}
	void HandleMovementAbilities(ref Vector2 velocity)
	{
		
	}
	void HandleMovement(ref Vector2 velocity)
	{
		if(!permissions.canWalkRun)
		{
			velocity.X = 0;
			state.horizontal = HorizontalState.none;
			return;
		}

		bool running = Input.IsActionPressed("dash");

		int direction = 0;
		if (Input.IsActionPressed("move_left")) direction -= 1;
		if (Input.IsActionPressed("move_right")) direction += 1;

		if (direction != 0)
		{
			velocity.X = direction * (running ? runSpeed: walkSpeed);
			state.horizontal = running ? HorizontalState.run: HorizontalState.walk;
		}
		else
		{
			velocity.X = 0;
			state.horizontal = HorizontalState.none;
		}
	}
	void HandleJump(double delta, ref Vector2 velocity)
	{
		if(IsOnFloor() && !IsOnCeiling())
		{
			airJumpCharge = maxAirJumpCharge;
			jumpDuration = 0;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			if(IsOnFloor())
			{
				// if(!IsOnCeiling())
				// {
					velocity.Y = JumpVelocity;
					state.vertical = VerticalState.groundJump;
				//}
			}
			else
			{
				if(airJumpCharge != 0)
				{
					velocity.Y = JumpVelocity;
					state.vertical = VerticalState.airJump;
					airJumpCharge --;
				}
			}
		}
		else if(Input.IsActionPressed("jump"))
		{
			if(state.IsJumping)
			{
				if(jumpDuration < (state.vertical == VerticalState.groundJump? maxJumpDuration:maxAirJumpDuration))
				{
					velocity.Y = JumpVelocity;
					jumpDuration += (float)delta;
				}
			}
		}
		else
		{
			jumpDuration = 0;
		}
	}
	
	void UpdateState()
	{
		if(IsOnFloor()) state.vertical = VerticalState.none;
		else if(Velocity.Y >= 0) state.vertical = VerticalState.fall;
	}
}
