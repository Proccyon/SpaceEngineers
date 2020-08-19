public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

float RotorSpeed = 0.8f;
float MaxRotorAngle = 75f;

float HingeRotations = 3f;
float MinHingeAngle = -80f;
float MaxHingeAngle = -30f;
float StationaryHingeAngle = -20f;


float PistonExtendDistance = 0.08f;
float OptimalDrillDistance = 4f;
float DrillDetectionRange = 35f;


public void RotateDrill(IMyMotorAdvancedStator Rotor, IMyMotorAdvancedStator Hinge, float RotorSpeed, float HingeFrequency, float MaxHingeAnlge, float MinHingeAnlge )
{
        
    var RotorAngle = Rotor.Angle*180/Math.PI;
    var HingeAngle = Hinge.Angle*180/Math.PI;


    if(RotorAngle >= 180)
    {
        RotorAngle -= 360;
    }

    if(RotorAngle >= MaxRotorAngle)
    {
        Rotor.TargetVelocityRPM = -RotorSpeed;
    }

    if(RotorAngle <= -MaxRotorAngle)
    {
        Rotor.TargetVelocityRPM = RotorSpeed;
    }

    if(HingeAngle >= MaxHingeAngle)
    {
        Hinge.TargetVelocityRPM = -RotorSpeed / HingeFrequency;
    }

    if(HingeAngle <= MinHingeAngle)
    {
        Hinge.TargetVelocityRPM = RotorSpeed / HingeFrequency;
    }

}

public bool ResetDrill(IMyMotorAdvancedStator Rotor, IMyMotorAdvancedStator Hinge,List<IMyPistonBase> PistonList, float RotorSpeed,float StationaryHingeAngle)
{        
    var RotorAngle = Rotor.Angle*180/Math.PI;
    var HingeAngle = Hinge.Angle*180/Math.PI;

    if(RotorAngle >= 180)
    {
        RotorAngle -= 360;
    }

    if(RotorAngle >= 0)
    {
        Rotor.TargetVelocityRPM = -RotorSpeed;
    }

    if(RotorAngle <= 0)
    {
        Rotor.TargetVelocityRPM = RotorSpeed;
    }

    if(HingeAngle >= StationaryHingeAngle)
    {
        Hinge.TargetVelocityRPM = -RotorSpeed;
    }

    if(HingeAngle <= StationaryHingeAngle)
    {
        Hinge.TargetVelocityRPM = RotorSpeed;
    }

    foreach(IMyPistonBase Piston in PistonList)
    {
        if(Piston.CustomName.Contains("DrillPiston"))
        {
            Piston.MinLimit = 0f;
            Piston.MaxLimit = 0f;
            Piston.Velocity = -Math.Abs(Piston.Velocity);
        }
    }



    return (HingeAngle >= -1f+StationaryHingeAngle && HingeAngle <= 1f+StationaryHingeAngle && RotorAngle >= -1f && RotorAngle <= 1f);

}

public void ExtendPistons(List<IMyPistonBase> PistonList, float ExtendAmount)
{
    foreach(IMyPistonBase Piston in PistonList)
    {
	if(Piston.CustomName.Contains("DrillPiston"))
	{
            Piston.MinLimit += PistonExtendDistance;
            Piston.MaxLimit += PistonExtendDistance;
	    Piston.Velocity = Math.Abs(Piston.Velocity);
	}
    }
}

public void ReducePistons(List<IMyPistonBase> PistonList, float ExtendAmount)
{
    foreach(IMyPistonBase Piston in PistonList)
    {
	if(Piston.CustomName.Contains("DrillPiston"))
	{
        Piston.MinLimit -= PistonExtendDistance;
        Piston.MaxLimit -= PistonExtendDistance;
	Piston.Velocity = -Math.Abs(Piston.Velocity);
	}
    }
}

public void Main(string argument, UpdateType updateSource)
{

List<IMyTerminalBlock> RotorList = new List<IMyTerminalBlock>();  
GridTerminalSystem.SearchBlocksOfName("DrillRotor", RotorList);
var DrillRotor =  (IMyMotorAdvancedStator)RotorList[0];

List<IMyTerminalBlock> HingeList2 = new List<IMyTerminalBlock>();  
GridTerminalSystem.SearchBlocksOfName("DrillHinge2", HingeList2);
var Hinge2 =  (IMyMotorAdvancedStator)HingeList2[0];

List<IMyTerminalBlock> DrillList = new List<IMyTerminalBlock>();  
GridTerminalSystem.SearchBlocksOfName("StationDrill", DrillList);
var Drill =  (IMyShipDrill)DrillList[0];

List<IMyTerminalBlock> UpperCameraList = new List<IMyTerminalBlock>();  
GridTerminalSystem.SearchBlocksOfName("UpperDrillCamera", UpperCameraList);
var UpperCamera =  (IMyCameraBlock)UpperCameraList[0];

List<IMyTerminalBlock> LowerCameraList = new List<IMyTerminalBlock>();  
GridTerminalSystem.SearchBlocksOfName("LowerDrillCamera", LowerCameraList);
var LowerCamera =  (IMyCameraBlock)LowerCameraList[0];

List<IMyPistonBase> PistonList = new List<IMyPistonBase>(); 
GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(PistonList);


if(argument == "On/Off")
{
    if(Storage == "RotateDrill")
    {
        Storage = "ResetDrill";
        Drill.Enabled = false;
    }
    else
    {
        Storage = "RotateDrill";
        Drill.Enabled = true;
        DrillRotor.TargetVelocityRPM = -RotorSpeed;
        
    }
}

if(argument == "ExtendPiston")
    {
    	foreach(IMyPistonBase Piston in PistonList)
	{
 		Piston.MinLimit += PistonExtendDistance;
		Piston.MaxLimit += PistonExtendDistance;
	}
    }

if(Storage == "RotateDrill")
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    RotateDrill(DrillRotor,Hinge2,RotorSpeed,HingeRotations,MaxHingeAngle,MinHingeAngle);

    try
    {

        UpperCamera.EnableRaycast = true;
        var UpperInfo = UpperCamera.Raycast(DrillDetectionRange,0f,0f);
        LowerCamera.EnableRaycast = true;
        var LowerInfo = LowerCamera.Raycast(DrillDetectionRange,0f,0f);

        var UpperDrillDistance = ((Vector3D)UpperInfo.HitPosition-Drill.GetPosition()).Length();
        var LowerDrillDistance = ((Vector3D)LowerInfo.HitPosition-Drill.GetPosition()).Length();

        var DrillDistance = Math.Min(UpperDrillDistance,LowerDrillDistance);

	Echo(UpperDrillDistance.ToString());
	Echo(LowerDrillDistance.ToString());


        if(DrillDistance >= OptimalDrillDistance)
        {
 	    ExtendPistons(PistonList,PistonExtendDistance);
	    
        }
        else
        {
            ReducePistons(PistonList,PistonExtendDistance);
        }

    }
    catch
    {
    }

}

if(Storage == "ResetDrill")
{
    bool StopScript = ResetDrill(DrillRotor,Hinge2,PistonList,RotorSpeed,StationaryHingeAngle);
    if(StopScript)
    {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        DrillRotor.TargetVelocityRPM = 0;
        Hinge2.TargetVelocityRPM = 0;
    }
}

Echo("Status: "+Storage);

}
