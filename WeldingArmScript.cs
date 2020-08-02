public Program()
{

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

    public float X0;
    public float Y0;
    public float Z0;

    public float X1;
    public float Y1;
    public float Z1;

    public Vector3D X_Vector;
    public Vector3D Y_Vector;
    public Vector3D Z_Vector;

    public float Ym;
    public float Zm;

    public float NewHinge1Angle; 
    public float NewHinge2Angle;

    public float NewPistonY1Length;
    public float NewPistonY2Length; 


    public WelderArm(IMyBatteryBlock BottomLeftBattery,IMyBatteryBlock BottomRightBattery,IMyBatteryBlock TopLeftBattery,IMyPistonBase PistonX1,IMyPistonBase PistonX2,IMyPistonBase PistonY1,IMyPistonBase PistonY2, IMyMotorAdvancedStator Hinge1, IMyMotorAdvancedStator Hinge2, IMyMotorAdvancedStator Hinge3)
    {


    //-----GetVectorFrame-----//

    var X_Vector = (BottomLeftBattery.GetPosition() - BottomRightBattery.GetPosition());
    X_Vector /= X_Vector.Length();

    var Y_Vector = (TopLeftBattery.GetPosition() - BottomLeftBattery.GetPosition());
    Y_Vector /= Y_Vector.Length();

    var Z_Vector = -Vector3D.Cross(X_Vector,Y_Vector);

    this.X1 = 20f;
    this.Y1 = 20f;
    this.Z1 = 20f;

    var MiddleHingeVector = this.GetMidleHingePosition();
    this.Ym = MiddleHingeVector[0];
    this.Zm = MiddleHingeVector[1];

    this.NewHinge1Angle = -(0.5f*(float)Math.PI  - (float)Math.Atan( (this.Zm) / (this.Ym)) );
    this.NewHinge2Angle = -(0.5f*(float)Math.PI  - (float)Math.Atan( (this.Z1 - this.Zm) / (this.Y1 - this.Ym)));

    this.NewPistonY1Length = (float)Math.Sqrt( (float)Math.Pow(this.Ym,2) + (float)Math.Pow(this.Zm,2) ) - 7.5f;
    this.NewPistonY2Length = (float)Math.Sqrt( (float)Math.Pow(this.Y1-Ym,2) + (float)Math.Pow(this.Z1-this.Zm,2)) - 7.5f;

    float PistonSpeed = 0.1f;
    float HingeSpeed = 0.4f;



    if(NewPistonY1Length >= 0 && NewPistonY2Length >= 0)
    {
        this.SetPiston(PistonY1,NewPistonY1Length,PistonSpeed);
        this.SetPiston(PistonY2,NewPistonY2Length,PistonSpeed);

        this.SetHinge(Hinge1,NewHinge1Angle,HingeSpeed);
        this.SetHinge(Hinge2,NewHinge2Angle,HingeSpeed);
    }

    }


    public float GoTo(float x1, float y1)
    {
        var MiddleHingeVector = this.GetMidleHingePosition();
        this.Ym = MiddleHingeVector[0];
        this.Zm = MiddleHingeVector[1];

        this.NewHinge1Angle = -(0.5f*(float)Math.PI  - (float)Math.Atan( (this.Zm) / (this.Ym)) );
        this.NewHinge2Angle = -(0.5f*(float)Math.PI  - (float)Math.Atan( (this.Z1 - this.Zm) / (this.Y1 - this.Ym)));

        this.NewPistonY1Length = (float)Math.Sqrt( (float)Math.Pow(this.Ym,2) + (float)Math.Pow(this.Zm,2) ) - 7.5f;
        this.NewPistonY2Length = (float)Math.Sqrt( (float)Math.Pow(this.Y1-Ym,2) + (float)Math.Pow(this.Z1-this.Zm,2)) - 7.5f;

        if(NewPistonY1Length >= 0 && NewPistonY2Length >= 0)
        {
    
            this.SetPiston(PistonY1,NewPistonY1Length,PistonSpeed);
            this.SetPiston(PistonY2,NewPistonY2Length,PistonSpeed);

            this.SetHinge(Hinge1,NewHinge1Angle,HingeSpeed);
            this.SetHinge(Hinge2,NewHinge2Angle,HingeSpeed);
        }
    }

    }


    public float ParabolicFormula(float y)
    {
        return 2 * y*this.Z1 / this.Y1 - (float)Math.Pow(y,2) * this.Z1 / (float)Math.Pow(this.Y1,2);
    }

    public List<float> GetMidleHingePosition()
    {

        float Ym = ( -(this.Y1 + 2f * (float)Math.Pow(this.Z1,2)/this.Y1) + (float)Math.Sqrt( (float)Math.Pow(this.Y1,2) + 2f * (float)Math.Pow(this.Z1,4) / (float)Math.Pow(this.Y1,2) + 2f * (float)Math.Pow(this.Z1,2))) / (-2f * (float)Math.Pow(this.Z1,2) / (float)Math.Pow(this.Y1,2));
        float Zm = this.ParabolicFormula(Ym);

        List<float> MiddleHingeVector = new List<float>();
        MiddleHingeVector.Add(Ym);
        MiddleHingeVector.Add(Zm);

        return MiddleHingeVector;
    }


    public void SetHinge(IMyMotorAdvancedStator Hinge, float Angle, float Speed)
    {

        Hinge.UpperLimitRad = Angle;
        Hinge.LowerLimitRad = Angle;

        if(Hinge.Angle >= Angle)
        {
            Hinge.TargetVelocityRPM = -Speed;
        }
        else
        {
            Hinge.TargetVelocityRPM = Speed;
        }
    }


    public float GetPistonLength(IMyPistonBase Piston)
    {
        string PistonInfo = Piston.DetailedInfo;
        PistonInfo = PistonInfo.Substring(28,4);
        var PistonInfoList = PistonInfo.Split('m');
        return float.Parse(PistonInfoList[0]);
    }

    public void SetPiston(IMyPistonBase Piston, float Distance, float Speed)
    {

        Piston.MinLimit = Distance;
        Piston.MaxLimit = Distance;

        if(GetPistonLength(Piston) >= Distance)
        {
            Piston.Velocity = -Speed;
        }
        else
        {
            Piston.Velocity = Speed;
        }

    }

}


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

    WelderArm MainWelder = new WelderArm(BottomLeftBattery,BottomRightBattery,TopLeftBattery,PistonX1,PistonX2,PistonY1,PistonY2,Hinge1,Hinge2,Hinge3);

    Echo("NewHinge1Angle = " + (MainWelder.NewHinge1Angle*180/(float)Math.PI).ToString());
    Echo("NewHinge2Angle = " + (MainWelder.NewHinge2Angle*180/(float)Math.PI).ToString());
    Echo("NewPistonY1Length = " + MainWelder.NewPistonY1Length.ToString());
    Echo("NewPistonY2Length = " + MainWelder.NewPistonY2Length.ToString());
    Echo("Ym = " + MainWelder.Ym.ToString());
    Echo("Zm = " + MainWelder.Zm.ToString());


}
