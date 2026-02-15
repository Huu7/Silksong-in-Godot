using Godot;
using System;
using System.Diagnostics.Tracing;

// public class Permissions
// {
// 	public bool canWalkRun = true;
// }

public enum HorizontalState
{
	none, walk, run
}
public enum VerticalState
{
	none, groundJump, airJump, wallJump, fall, floating
}
public enum AbilityState
{
	none, dash
}

public class PlayerState
{
	public HorizontalState horizontal {get; set;} = HorizontalState.none;
	public VerticalState vertical {get; set;} = VerticalState.none;
	public AbilityState ability {get; set;} = AbilityState.none;

	public bool IsJumping =>
		vertical is VerticalState.groundJump or VerticalState.airJump;
	public bool isWallSliding = false;
}

public class HasAbility
{
	public bool wallSlide = false;
	public bool faydownCloak = false;
}



public partial class Hornet : CharacterBody2D
{
	public PlayerState state = new();
	// public Permissions permissions = new();
	public HasAbility hasAbility = new();
	
	public const float walkSpeed = 300.0f, runSpeed = 600.0f;
	public const float JumpVelocity = -550.0f, wallJumpSpeed = 500;
	public const float maxJumpDuration = 0.25f, maxAirJumpDuration = 0.2f, maxWallJumpDuration = 0.15f, maxWallJumpBackDur = 0.15f;
	public const int maxAirJumpCharge = 1;
	int airJumpCharge;
	
	float jumpDuration = 0, wallJumpDuration = 0, wallJumpBackDur = 0;//, wallSlideDuration = 0;
	
	string debugInfo = "";
	int frameCount = 0;

	//Run Once
	public override void _Ready()
	{
		airJumpCharge = maxAirJumpCharge;
		hasAbility.wallSlide = true;
	}
	

	//Per Frame
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = new();

		Vector2 gravityVec = HandleGravity(delta);
		Vector2 movementVec = HandleMovement();
		Vector2 jumpVec = HandleJump(delta);
		Vector2 wallJumpVec = HandleWallSlideJump(delta);
		
		velocity.Y+=gravityVec.Y;
		velocity.X += movementVec.X;

		if(jumpVec != Vector2.Zero)
			velocity.Y = jumpVec.Y;	
		
		if(wallJumpVec != Vector2.Zero)
		{
			velocity.X = wallJumpVec.X;
			velocity.Y = wallJumpVec.Y;
		}

		preUpdateState(delta, ref velocity); //has access to velocity as it will clamp Y velocity when just hitting a wall

		Velocity = velocity;
		MoveAndSlide();
		postUpdateState();
		ShowDebug();
		frameCount++;
	}


	void ShowDebug()
	{
		string newdebugInfo =
		$"{Velocity.X:f6}, {Velocity.Y:f6} {(state.isWallSliding ? "sliding" : "notSlid")}    ⌚: {jumpDuration}\n" +
		$"     🧗⌚: {wallJumpDuration}  {state.horizontal}  {state.vertical}  🔋: {airJumpCharge}\n";

		if(newdebugInfo != debugInfo)
			GD.Print(frameCount, ": ",newdebugInfo);

		debugInfo = newdebugInfo;
	}
	Vector2 HandleGravity(double delta)
	{
		Vector2 gravityVec = new();

		float gravity = GetGravity().Y;

		if(state.isWallSliding && state.vertical == VerticalState.fall)
			gravity /= 6;

		if(!IsOnFloor())
		{
			gravityVec.Y = Velocity.Y + gravity * (float)delta;
		}
		
		return gravityVec;
	}
	Vector2 HandleMovement()
	{
		Vector2 moveVec = new();
		// if(state.isWallSliding)
		// 	return moveVec;

		bool running = Input.IsActionPressed("dash");

		int direction = 0;
		if (Input.IsActionPressed("move_left")) direction -= 1;
		if (Input.IsActionPressed("move_right")) direction += 1;

		if (direction != 0)
		{
			moveVec.X = direction * (running ? runSpeed: walkSpeed);
			state.horizontal = running ? HorizontalState.run: HorizontalState.walk;
		}
		else
		{
			state.horizontal = HorizontalState.none;
		}

		return moveVec;
	}
	Vector2 HandleJump(double delta)
	{
		Vector2 jumpVec = new();

		if(state.isWallSliding)
			return jumpVec;


		
		if (Input.IsActionJustPressed("jump"))
		{
			if(IsOnFloor())
			{
				jumpVec.Y = JumpVelocity;
				state.vertical = VerticalState.groundJump;
			}
			else
			{
				if(airJumpCharge != 0)
				{
					jumpVec.Y = JumpVelocity;
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
					jumpVec.Y = JumpVelocity;
					jumpDuration += (float)delta;
				}
			}
		}
		else
		{
			jumpDuration = 0;
		}

		return jumpVec;
	}
	Vector2 HandleWallSlideJump(double delta)
	{
		Vector2 vec = new();
		if(!hasAbility.wallSlide)
			return vec;
		

		Vector2 wNormal = GetWallNormal();

		if(Input.IsActionJustPressed("jump"))
		{
			if(state.isWallSliding)
			{
				state.vertical = VerticalState.wallJump;
				vec = (wNormal + Vector2.Up) * wallJumpSpeed;
			}
		}
		else if(Input.IsActionPressed("jump"))
		{
			if(state.vertical == VerticalState.wallJump)
			{
				if(wallJumpDuration < maxWallJumpDuration)
				{
					vec = (wNormal + Vector2.Up) * wallJumpSpeed;
					wallJumpDuration += (float)delta;
				}
				else if(!state.isWallSliding && wallJumpBackDur < maxWallJumpBackDur)
				{
					int dir = 0;
					if(Input.IsActionPressed("move_left")) dir = -1;
					else if(Input.IsActionPressed("move_right")) dir = 1;

					if(dir == -wNormal.X) vec = (-wNormal + Vector2.Up) * wallJumpSpeed;

					wallJumpBackDur += (float)delta;
				}
			}
		}
		else
		{
			wallJumpDuration = 0;
			wallJumpBackDur = maxWallJumpBackDur;
		}

		return vec;
	}

	void preUpdateState(double delta, ref Vector2 velocity)
	{
		if(IsOnWall() && !IsOnFloor())
		{
			if(!state.isWallSliding)
			{
				//wallSlideDuration = 0;
				velocity.Y = 0;
				jumpDuration = 0;
				state.isWallSliding = true;
				state.vertical = VerticalState.fall;
			}
			else
			{
				//wallSlideDuration += (float)delta;
			}

			//wallJumpDuration = 0;
		}
	}
	void postUpdateState()
	{
		if(state.isWallSliding)
		{
			if(!IsOnWall() || IsOnFloor())
			{
				state.isWallSliding = false;
			}
			else
			{
				wallJumpDuration = 0;
			}
		}

		if(!state.IsJumping)
		{
			jumpDuration = 0;
		}


		if(IsOnFloor())
		{
			state.vertical = VerticalState.none;
			airJumpCharge = maxAirJumpCharge;
		}
		if(state.isWallSliding)
		{
			airJumpCharge = maxAirJumpCharge;
		}
		else if(Velocity.Y >= 0) state.vertical = VerticalState.fall;
	}
}