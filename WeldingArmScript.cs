public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save()
{

}


//Returns block of given name. If more blocks are found with same name, returns first one in list.
public IMyTerminalBlock GetBlockByName(string BlockName)
{

    List<IMyTerminalBlock> BlockList = new List<IMyTerminalBlock>();  
    GridTerminalSystem.SearchBlocksOfName(BlockName, BlockList);
    return  BlockList[0];
}

public class WelderArm
{

    public IMyBatteryBlock BottomLeftBattery;
    public IMyBatteryBlock BottomRightBattery;
    public IMyBatteryBlock TopLeftBattery;
    public IMyPistonBase PistonX1;
    public IMyPistonBase PistonX2;
    public IMyPistonBase PistonY1;
    public IMyPistonBase PistonY2;
    public IMyMotorAdvancedStator Hinge1;
    public IMyMotorAdvancedStator Hinge2;
    public IMyMotorAdvancedStator Hinge3;

    public Vector3D X_Vector;
    public Vector3D Y_Vector;
    public Vector3D Z_Vector;

    public float WelderMoveSpeed = 2.5f;

    public Program MyProgram;

    public WelderArm(Program MyProgram, IMyBatteryBlock BottomLeftBattery,IMyBatteryBlock BottomRightBattery,IMyBatteryBlock TopLeftBattery,IMyPistonBase PistonX1,IMyPistonBase PistonX2,IMyPistonBase PistonY1,IMyPistonBase PistonY2, IMyMotorAdvancedStator Hinge1, IMyMotorAdvancedStator Hinge2, IMyMotorAdvancedStator Hinge3)
    {


        this.MyProgram = MyProgram;

        //-----GetVectorFrame-----//

        this.X_Vector = (BottomLeftBattery.GetPosition() - BottomRightBattery.GetPosition());
        this.X_Vector /= this.X_Vector.Length();

        this.Y_Vector = (TopLeftBattery.GetPosition() - BottomLeftBattery.GetPosition());
        this.Y_Vector /= this.Y_Vector.Length();

        this.Z_Vector = -Vector3D.Cross(this.X_Vector,this.Y_Vector);

        this.BottomLeftBattery = BottomLeftBattery;
        this.BottomRightBattery = BottomRightBattery;
        this.TopLeftBattery = TopLeftBattery;
        this.PistonX1 = PistonX1;
        this.PistonX2 = PistonX2;
        this.PistonY1 = PistonY1;
        this.PistonY2 = PistonY2;
        this.Hinge1 = Hinge1;
        this.Hinge2 = Hinge2;
        this.Hinge3 = Hinge3;

    }


    //Moves Tip of welder to given location
    public void GoTo(float Y1, float Z1)
    {
        //Gets new position of middle hinge
        var MiddleHingeVector = this.GetMiddleHingePosition(Y1,Z1);
        float Ym = MiddleHingeVector[0];
        float Zm = MiddleHingeVector[1];

        //Calculates new hinge angles and piston lengths
        float NewHinge1Angle = -(0.5f*(float)Math.PI  - (float)Math.Atan( (Zm) / (Ym)) );
        float NewHinge2Angle = -NewHinge1Angle+(float)Math.Atan( (Z1 - Zm) / (Y1 - Ym))-0.5f*(float)Math.PI;

        float NewPistonY1Length = (float)Math.Sqrt( (float)Math.Pow(Ym,2) + (float)Math.Pow(Zm,2) ) - 7.5f;
        float NewPistonY2Length = (float)Math.Sqrt( (float)Math.Pow(Y1-Ym,2) + (float)Math.Pow(Z1-Zm,2)) - 7.5f;

        //Distance from location
        float DestinationDistance = this.GetDistanceFromDestination(Y1,Z1); 

        //this.MyProgram.Echo("Ym = " + Ym.ToString());
        //this.MyProgram.Echo("Zm = " + Zm.ToString());
        this.MyProgram.Echo("NewHinge1Angle = " + (NewHinge1Angle*180/(float)Math.PI).ToString());
        this.MyProgram.Echo("NewHinge2Angle = " + (NewHinge2Angle*180/(float)Math.PI).ToString());
        this.MyProgram.Echo("NewPistonY1Length = " + NewPistonY1Length.ToString());
        this.MyProgram.Echo("NewPistonY2Length = " + NewPistonY2Length.ToString());
        this.MyProgram.Echo("Y1 = " + Y1.ToString());
        this.MyProgram.Echo("Z1 = " + Z1.ToString());
        this.MyProgram.Echo("RealY1 = " + Vector3D.Dot(Hinge3.GetPosition() - Hinge1.GetPosition(), this.Y_Vector).ToString());
        this.MyProgram.Echo("RealZ1 = " + Vector3D.Dot(Hinge3.GetPosition() - Hinge1.GetPosition(), this.Z_Vector).ToString());
        this.MyProgram.Echo("TickLength = " + (DestinationDistance/this.WelderMoveSpeed).ToString());

        //Does nothing if destination is too close to arm base
        if(NewPistonY1Length >= 0 && NewPistonY2Length >= 0)
        {
            
            float JourneyTime = (DestinationDistance / this.WelderMoveSpeed);
            if(JourneyTime < 0.25f)
            {
                JourneyTime = 0.25f;
            }

            //Calculates speed so that all parts take the same amount of time to move
            float Hinge1Speed = (NewHinge1Angle - this.Hinge1.Angle) / JourneyTime;
            float Hinge2Speed = (NewHinge2Angle - this.Hinge2.Angle) / JourneyTime;
            float PistonY1Speed = (NewPistonY1Length - this.GetPistonLength(this.PistonY1)) / JourneyTime;
            float PistonY2Speed = (NewPistonY2Length - this.GetPistonLength(this.PistonY2)) / JourneyTime;

            //Sets speed as to move to new position
            this.SetHinge(this.Hinge1,NewHinge1Angle,Hinge1Speed);
            this.SetHinge(this.Hinge2,NewHinge2Angle,Hinge2Speed);

            this.SetPiston(this.PistonY1,NewPistonY1Length,PistonY1Speed);
            this.SetPiston(this.PistonY2,NewPistonY2Length,PistonY2Speed);

        }

    }

    //Gets the distance from tip of welder to destination
    public float GetDistanceFromDestination(float Y1, float Z1)
    {

        float PistonY1Length = this.GetPistonLength(this.PistonY1);
        float PistonY2Length = this.GetPistonLength(this.PistonY2);
        float Hinge1Angle = this.Hinge1.Angle;
        float Hinge2Angle = this.Hinge2.Angle;

        float Y = (PistonY1Length+7.5f)*(float)Math.Sin(-Hinge1Angle)+(PistonY2Length+7.5f)*(float)Math.Cos((Hinge1Angle+Hinge2Angle+0.5f*(float)Math.PI));
        float Z = (PistonY1Length+7.5f)*(float)Math.Cos(-Hinge1Angle)+(PistonY2Length+7.5f)*(float)Math.Sin((Hinge1Angle+Hinge2Angle+0.5f*(float)Math.PI));

        //this.MyProgram.Echo(Y.ToString());
        //this.MyProgram.Echo(Z.ToString());
        return (float)Math.Sqrt((float)Math.Pow(Y1-Y,2) + (float)Math.Pow(Z1-Z,2));
    }

    //Welder arm follows a parabola to decrease free parameters
    public float ParabolicFormula(float Y, float Y1, float Z1)
    {
        return 2 * Y * Z1 / Y1 - (float)Math.Pow(Y,2) * Z1 / (float)Math.Pow(Y1,2);
    }

    //Gets position of middle hinge
    public List<float> GetMiddleHingePosition(float Y1, float Z1)
    {

        float Ym = ( -(Y1 + 2f * (float)Math.Pow(Z1,2) / Y1) + (float)Math.Sqrt( (float)Math.Pow(Y1,2) + 2f * (float)Math.Pow(Z1,4) / (float)Math.Pow(Y1,2) + 2f * (float)Math.Pow(Z1,2))) / (-2f * (float)Math.Pow(Z1,2) / (float)Math.Pow(Y1,2));
        float Zm = this.ParabolicFormula(Ym,Y1,Z1);

        List<float> MiddleHingeVector = new List<float>();
        MiddleHingeVector.Add(Ym);
        MiddleHingeVector.Add(Zm);

        return MiddleHingeVector;
    }

    public void SetHinge(IMyMotorAdvancedStator Hinge, float Angle, float Speed)
    {

        //Hinge.UpperLimitRad = Angle;
        //Hinge.LowerLimitRad = Angle;
        Hinge.TargetVelocityRad = Speed;
    }

    public void SetPiston(IMyPistonBase Piston, float Distance, float Speed)
    {

        //Piston.MinLimit = Distance;
        //Piston.MaxLimit = Distance;
        Piston.Velocity = Speed;
    
    }

    //Gets current length of a piston
    public float GetPistonLength(IMyPistonBase Piston)
    {
        string PistonInfo = Piston.DetailedInfo;
        PistonInfo = PistonInfo.Substring(28,4);
        var PistonInfoList = PistonInfo.Split('m');
        return float.Parse(PistonInfoList[0]);
    }

}

public Vector3D GetCoordFromPath(float s, List<Vector3D> CoordList)
{

    float S_Path = 0f;
    Vector3D LastCoord = CoordList[0];
    float SegmentLength = 0f;

    foreach(Vector3D Coord in CoordList)
    {
        //Length betweeb Coord and LastCoord
        SegmentLength = (float)(Coord-LastCoord).Length();

        //Total lentgh of path between CoordList[0] and Coord
        S_Path += SegmentLength;
        
        if(s < S_Path)
        {

            float TraversedSegmentLength = SegmentLength + s - S_Path;
            return TraversedSegmentLength * (Coord - LastCoord) / SegmentLength + LastCoord;

        }

        LastCoord = Coord;
    }

    return LastCoord;

}

public void FollowPath(WelderArm WeldArm, List<Vector3D> CoordList, float CloseDistance = 0.5f)
{

    Vector3D OldCoord = GetCoordFromPath(s,CoordList);
    float OldDistance = WeldArm.GetDistanceFromDestination((float)OldCoord.Y,(float)OldCoord.Z);

    //Echo(OldDistance.ToString());

    if(OldDistance <= CloseDistance)
    {
        s += WeldArm.WelderMoveSpeed * 0.1f;
    }

    Vector3D NewCoord = GetCoordFromPath(s,CoordList);
    WeldArm.GoTo((float)NewCoord.Y,(float)NewCoord.Z);

    //float NewDistance = WeldArm.GetDistanceFromDestination(NewCoord[1],NewCoord[2]);


}



public float s = 0f;

public void Main(string argument, UpdateType updateSource)
{

    //-----BlockInitialization-----//

    IMyBatteryBlock BottomLeftBattery = (IMyBatteryBlock)GetBlockByName("BottomLeftBattery");
    IMyBatteryBlock BottomRightBattery = (IMyBatteryBlock)GetBlockByName("BottomRightBattery");
    IMyBatteryBlock TopLeftBattery = (IMyBatteryBlock)GetBlockByName("TopLeftBattery");

    IMyPistonBase PistonX1 = (IMyPistonBase)GetBlockByName("WeldPistonX1");
    IMyPistonBase PistonX2 = (IMyPistonBase)GetBlockByName("WeldPistonX2");
    IMyPistonBase PistonY1 = (IMyPistonBase)GetBlockByName("WeldPistonY1");
    IMyPistonBase PistonY2 = (IMyPistonBase)GetBlockByName("WeldPistonY2");

    IMyMotorAdvancedStator Hinge1 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge1");
    IMyMotorAdvancedStator Hinge2 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge2");
    IMyMotorAdvancedStator Hinge3 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge3");

    //-----GetVectorFrame-----//

    WelderArm MainWelder = new WelderArm(this,BottomLeftBattery,BottomRightBattery,TopLeftBattery,PistonX1,PistonX2,PistonY1,PistonY2,Hinge1,Hinge2,Hinge3);

    List<Vector3D> WelderPath = new List<Vector3D>();
    WelderPath.Add(new Vector3D(15f,15f,15f));
    WelderPath.Add(new Vector3D(15f,30f,15f));
    WelderPath.Add(new Vector3D(15f,30f,10f));
    WelderPath.Add(new Vector3D(15f,15f,10f));
    WelderPath.Add(new Vector3D(15f,15f,15f));

    FollowPath(MainWelder,WelderPath);

    Echo("s = " + s.ToString());


}
