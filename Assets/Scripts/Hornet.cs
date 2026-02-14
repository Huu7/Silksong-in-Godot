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
	public Permissions permissions = new();
	public HasAbility hasAbility = new();
	
	public const float walkSpeed = 300.0f, runSpeed = 600.0f;
	public const float JumpVelocity = -550.0f, wallJumpSpeed = 700;
	public const float maxJumpDuration = 0.25f, maxAirJumpDuration = 0.2f, maxWallJumpDuration = 0.2f;
	public const int maxAirJumpCharge = 1;

	int airJumpCharge;
	float jumpDuration = 0;
	float wallJumpDuration = 0;
	
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
		HandleWallSlide();
		Vector2 wallJumpVec = HandleWallJump(delta);
		

		
		velocity.Y+=gravityVec.Y;
		velocity.X += movementVec.X;

		if(jumpVec.Y != 0)
			velocity.Y = jumpVec.Y;	
		if(wallJumpVec != Vector2.Zero)
		{
			velocity.X += wallJumpVec.X;
			velocity.Y = wallJumpVec.Y;
		}

		Velocity = velocity;
		MoveAndSlide();
		UpdateState();
		ShowDebug();
		frameCount++;
	}


	void ShowDebug()
	{
		string newdebugInfo =
		$"🚶‍♂️‍➡️: {permissions.canWalkRun}  {(state.isWallSliding ? "sliding" : "notSliding")}  {Velocity}  ⌚: {jumpDuration}\n" +
		$"     {state.horizontal}  {state.vertical}  🔋: {airJumpCharge}";

		if(newdebugInfo != debugInfo)
			GD.Print(frameCount, ": ",newdebugInfo);

		debugInfo = newdebugInfo;
	}
	Vector2 HandleGravity(double delta)
	{
		Vector2 gravityVec = new();

		float gravity = GetGravity().Y;

		if(state.isWallSliding && state.vertical == VerticalState.fall)
			gravity /= 10;

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
	void HandleWallSlide()
	{
		if (!(
			hasAbility.wallSlide &&
			state.vertical != VerticalState.wallJump))
			return;
		
		if(IsOnWall() && !IsOnFloor())
		{
			state.isWallSliding = true;
		}
	}
	Vector2 HandleWallJump(double delta)
	{
		Vector2 vec = new();

		if(!hasAbility.wallSlide)
			return vec;


		if(Input.IsActionJustPressed("jump"))
		{
			if(state.isWallSliding)
			{
				state.vertical = VerticalState.wallJump;
				vec = (GetWallNormal() + Vector2.Up) * wallJumpSpeed;
			}
		}
		else if(Input.IsActionPressed("jump"))
		{
			if(state.vertical == VerticalState.wallJump)
			{
				if(wallJumpDuration < 0.3)
				{
					vec = (GetWallNormal() + Vector2.Up) * wallJumpSpeed;
					wallJumpDuration += (float)delta;
				}
			}
		}
		else
		{
			wallJumpDuration = 0;
		}

		return vec;
	}

	void UpdateState()
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
			airJumpCharge = maxAirJumpCharge;
			jumpDuration = 0;
		}


		if(IsOnFloor()) state.vertical = VerticalState.none;
		else if(Velocity.Y >= 0) state.vertical = VerticalState.fall;
	}
}
