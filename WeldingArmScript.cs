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

    public IMyPistonBase PistonX1;
    public IMyPistonBase PistonX2;
    public IMyPistonBase PistonY1;
    public IMyPistonBase PistonY2;
    public IMyMotorAdvancedStator Hinge1;
    public IMyMotorAdvancedStator Hinge2;
    public IMyMotorAdvancedStator Hinge3;

    public Program MyProgram;

    public WelderArm(Program MyProgram,IMyPistonBase PistonX1,IMyPistonBase PistonX2,IMyPistonBase PistonY1,IMyPistonBase PistonY2, IMyMotorAdvancedStator Hinge1, IMyMotorAdvancedStator Hinge2, IMyMotorAdvancedStator Hinge3)
    {

        this.MyProgram = MyProgram;

        this.PistonX1 = PistonX1;
        this.PistonX2 = PistonX2;
        this.PistonY1 = PistonY1;
        this.PistonY2 = PistonY2;
        this.Hinge1 = Hinge1;
        this.Hinge2 = Hinge2;
        this.Hinge3 = Hinge3;

    }

    public List<float> GetNewSystemParameters(float X1, float Y1, float Z1, Func<float,float,List<float>> MiddleHingeMethod)
    {
        //Calculates position of middle hinge
        var MiddleHingeVector = MiddleHingeMethod(Y1,Z1);
        float Ym = MiddleHingeVector[0];
        float Zm = MiddleHingeVector[1];

        //Calculates new hinge angles and piston lengths
        float NewHinge1Angle = -(float)Math.Atan( (Ym) / (Zm));
        float NewHinge2Angle = -NewHinge1Angle+(float)Math.Atan( (Z1 - Zm) / (Y1 - Ym))-0.5f*(float)Math.PI;
        float NewHinge3Angle = 0.5f*(float)Math.PI + NewHinge1Angle + NewHinge2Angle;

        float NewPistonY1Length = (float)Math.Sqrt( (float)Math.Pow(Ym,2) + (float)Math.Pow(Zm,2) ) - 7.5f;
        float NewPistonY2Length = (float)Math.Sqrt( (float)Math.Pow(Y1-Ym,2) + (float)Math.Pow(Z1-Zm,2)) - 7.5f;

        List<float> SystemParameterList = new List<float>();
        SystemParameterList.Add(NewHinge1Angle);
        SystemParameterList.Add(NewHinge2Angle);
        SystemParameterList.Add(NewHinge3Angle);
        SystemParameterList.Add(NewPistonY1Length);
        SystemParameterList.Add(NewPistonY2Length);

        return SystemParameterList;


    }



    //Moves Tip of welder to given location
    public void GoTo(float X1, float Y1, float Z1, float MoveSpeed, Func<float,float,List<float>> MiddleHingeMethod)
    {

        int Iterations = 5;
        float X1_Adjusted = X1;
        float Y1_Adjusted = Y1;
        float Z1_Adjusted = Z1 + 5f;

        List<float> SystemParameterList;
        float NewHinge1Angle = 0; 
        float NewHinge2Angle = 0;
        float NewHinge3Angle = 0;
        float NewPistonY1Length = 0;
        float NewPistonY2Length = 0;

        for (int i = 0; i < Iterations; i++) 
        {

            SystemParameterList = this.GetNewSystemParameters(X1_Adjusted,Y1_Adjusted,Z1_Adjusted,MiddleHingeMethod);

            NewHinge1Angle = SystemParameterList[0];
            NewHinge2Angle = SystemParameterList[1];
            NewHinge3Angle = SystemParameterList[2];
            NewPistonY1Length = SystemParameterList[3];
            NewPistonY2Length = SystemParameterList[4];

            Y1_Adjusted = Y1 - 2.5f * (float)Math.Sin(NewHinge3Angle);
            Z1_Adjusted = Z1 + 2.5f + 2.5f * (float)Math.Cos(NewHinge3Angle);

        }

        //Distance from location
        float DestinationDistance = this.GetDistanceFromDestination(X1,Y1,Z1); 

        this.MyProgram.Echo("NewHinge1Angle = " + (NewHinge1Angle*180/(float)Math.PI).ToString());
        this.MyProgram.Echo("NewHinge2Angle = " + (NewHinge2Angle*180/(float)Math.PI).ToString());
        this.MyProgram.Echo("NewHinge3Angle = " + (NewHinge3Angle*180/(float)Math.PI).ToString());
        this.MyProgram.Echo("NewPistonY1Length = " + NewPistonY1Length.ToString());
        this.MyProgram.Echo("NewPistonY2Length = " + NewPistonY2Length.ToString());
        this.MyProgram.Echo("DestinationDistance = " + DestinationDistance.ToString());
        this.MyProgram.Echo("Y1_Adjusted = " + Y1_Adjusted.ToString());
        this.MyProgram.Echo("Z1_Adjusted = " + Z1_Adjusted.ToString());
        this.MyProgram.Echo("Y1 = " + Y1.ToString());
        this.MyProgram.Echo("Z1 = " + Z1.ToString());

        //Does nothing if destination is too close to arm base
        if(NewPistonY1Length >= 0 && NewPistonY2Length >= 0)
        {
            
            float JourneyTime = (DestinationDistance / MoveSpeed);
            if(JourneyTime < 0.25f)
            {
                JourneyTime = 0.25f;
            }

            //Calculates speed so that all parts take the same amount of time to movepis
            float Hinge1Speed = (NewHinge1Angle - this.Hinge1.Angle) / JourneyTime;
            float Hinge2Speed = (NewHinge2Angle - this.Hinge2.Angle) / JourneyTime;
            float Hinge3Speed = (NewHinge3Angle - this.Hinge3.Angle) / JourneyTime;
            float PistonY1Speed = (NewPistonY1Length - this.GetPistonLength(this.PistonY1)) / JourneyTime;
            float PistonY2Speed = (NewPistonY2Length - this.GetPistonLength(this.PistonY2)) / JourneyTime;
            float PistonX1Speed = (X1 / 2 - this.GetPistonLength(this.PistonX1)) / JourneyTime;
            float PistonX2Speed = (X1 / 2 - this.GetPistonLength(this.PistonX2)) / JourneyTime;

            //Sets speed as to move to new position
            this.SetHinge(this.Hinge1,NewHinge1Angle,Hinge1Speed);
            this.SetHinge(this.Hinge2,NewHinge2Angle,Hinge2Speed);
            this.SetHinge(this.Hinge3,NewHinge3Angle,Hinge3Speed);

            this.SetPiston(this.PistonY1,NewPistonY1Length,PistonY1Speed);
            this.SetPiston(this.PistonY2,NewPistonY2Length,PistonY2Speed);

            this.SetPiston(this.PistonX1, X1 / 2, PistonX1Speed);
            this.SetPiston(this.PistonX2, X1 / 2, PistonX2Speed);

        }

    }

    //Gets the distance from tip of welder to destination
    public float GetDistanceFromDestination(float X1, float Y1, float Z1)
    {

        float PistonY1Length = this.GetPistonLength(this.PistonY1);
        float PistonY2Length = this.GetPistonLength(this.PistonY2);
        float PistonX1Length = this.GetPistonLength(this.PistonX1);
        float PistonX2Length = this.GetPistonLength(this.PistonX2);
        float Hinge1Angle = this.Hinge1.Angle;
        float Hinge2Angle = this.Hinge2.Angle;
        float Hinge3Angle = this.Hinge3.Angle;


        float X = PistonX1Length + PistonX2Length;
        float Y = (PistonY1Length+7.5f)*(float)Math.Sin(-Hinge1Angle)+(PistonY2Length+7.5f)*(float)Math.Cos((Hinge1Angle+Hinge2Angle+0.5f*(float)Math.PI)) + 2.5f*(float)Math.Sin(Hinge3Angle);
        float Z = (PistonY1Length+7.5f)*(float)Math.Cos(-Hinge1Angle)+(PistonY2Length+7.5f)*(float)Math.Sin((Hinge1Angle+Hinge2Angle+0.5f*(float)Math.PI)) -2.5f - 2.5f*(float)Math.Cos(Hinge3Angle);

        return (float)Math.Sqrt((float)Math.Pow(X1-X,2) + (float)Math.Pow(Y1-Y,2) + (float)Math.Pow(Z1-Z,2));
    }

    //Welder arm follows a parabola to decrease free parameters
    public float ParabolicFormula(float Y, float Y1, float Z1)
    {
        return 2 * Y * Z1 / Y1 - (float)Math.Pow(Y,2) * Z1 / (float)Math.Pow(Y1,2);
    }

    //Gets position of middle hinge
    public List<float> GetMiddleHingePositionP1(float Y1, float Z1)
    {

        float Ym = ( -(Y1 + 2f * (float)Math.Pow(Z1,2) / Y1) + (float)Math.Sqrt( (float)Math.Pow(Y1,2) + 2f * (float)Math.Pow(Z1,4) / (float)Math.Pow(Y1,2) + 2f * (float)Math.Pow(Z1,2))) / (-2f * (float)Math.Pow(Z1,2) / (float)Math.Pow(Y1,2));
        float Zm = this.ParabolicFormula(Ym,Y1,Z1);

        List<float> MiddleHingeVector = new List<float>();
        MiddleHingeVector.Add(Ym);
        MiddleHingeVector.Add(Zm);

        return MiddleHingeVector;
    }

    //Gets position of middle hinge
    public List<float> GetMiddleHingePositionP2(float Y1, float Z1)
    {

        float Alpha = 0.7f;
        float Ym = Y1*Alpha;
        float Zm = Z1*Alpha*Alpha / (2 * Alpha - 1);

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

        //if( Math.Abs(GetPistonLength(Piston) - Distance) <= 0.1f)
        //{
        //    Piston.Velocity = 0f;
        //}
        //else
        //{
        //    this.MyProgram.Echo(Math.Abs(GetPistonLength(Piston) - Distance).ToString());
        //    Piston.Velocity = Speed;
        //}
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

public Vector3D GetCoordFromPath(float s, List<Vector3D> CoordList, List<float> SpeedList, out float NewSpeed)
{

    float S_Path = 0f;
    Vector3D LastCoord = CoordList[0];
    float SegmentLength = 0f;

    //foreach(Vector3D Coord in CoordList)
    for(int i=0; i<CoordList.Count; i++)
    {

        Vector3D Coord = CoordList[i];

        //Length between Coord and LastCoord
        SegmentLength = (float)(Coord-LastCoord).Length();

        //Total lentgh of path between CoordList[0] and Coord
        S_Path += SegmentLength;
        
        if(s <= S_Path)
        {

            NewSpeed = SpeedList[i];
            if(SegmentLength == 0)
            {
                return CoordList[0];
            }
            else
            {
                float TraversedSegmentLength = SegmentLength + s - S_Path;
                return TraversedSegmentLength * (Coord - LastCoord) / SegmentLength + LastCoord;
            }

        }

        LastCoord = Coord;
    }

    NewSpeed = SpeedList[SpeedList.Count-1];
    return LastCoord;

}



public void FollowPath(WelderArm WeldArm, List<Vector3D> CoordList, List<float> SpeedList, float CloseDistance = 0.5f)
{

    float NewSpeed = 0f;
    Vector3D OldCoord = GetCoordFromPath(s,CoordList,SpeedList,out NewSpeed);
    float OldDistance = WeldArm.GetDistanceFromDestination((float)OldCoord.X, (float)OldCoord.Y, (float)OldCoord.Z);

    if(OldDistance <= CloseDistance)
    {
        s += NewSpeed*0.1f;
    }

    Vector3D NewCoord = GetCoordFromPath(s,CoordList,SpeedList,out NewSpeed);
    WeldArm.GoTo((float)NewCoord.X, (float)NewCoord.Y,(float)NewCoord.Z, NewSpeed, WeldArm.GetMiddleHingePositionP1);

}



public float s = 0f;

public void Main(string argument, UpdateType updateSource)
{

    //-----BlockInitialization-----//

    IMyPistonBase PistonX1 = (IMyPistonBase)GetBlockByName("WeldPistonX1");
    IMyPistonBase PistonX2 = (IMyPistonBase)GetBlockByName("WeldPistonX2");
    IMyPistonBase PistonY1 = (IMyPistonBase)GetBlockByName("WeldPistonY1");
    IMyPistonBase PistonY2 = (IMyPistonBase)GetBlockByName("WeldPistonY2");

    IMyMotorAdvancedStator Hinge1 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge1");
    IMyMotorAdvancedStator Hinge2 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge2");
    IMyMotorAdvancedStator Hinge3 = (IMyMotorAdvancedStator)GetBlockByName("WeldingHinge3");

    //-----GetVectorFrame-----//

    WelderArm MainWelder = new WelderArm(this,PistonX1,PistonX2,PistonY1,PistonY2,Hinge1,Hinge2,Hinge3);

    List<Vector3D> WelderPath = new List<Vector3D>();
    WelderPath.Add(new Vector3D(0f,22.5f,11f));
    WelderPath.Add(new Vector3D(10.75f,22.5f,11f));
    WelderPath.Add(new Vector3D(10.75f,22.5f,3.5f));
    WelderPath.Add(new Vector3D(10.75f,22.5f,11f));
    WelderPath.Add(new Vector3D(0f,22.5f,11f));

    List<float> WelderSpeedList = new List<float>();
    WelderSpeedList.Add(2f);
    WelderSpeedList.Add(2f);
    WelderSpeedList.Add(1.5f);
    WelderSpeedList.Add(0.15f);
    WelderSpeedList.Add(2f);

    FollowPath(MainWelder,WelderPath,WelderSpeedList);

    Echo("s = " + s.ToString());


}
