[System.Serializable]
public class GameData
{
    private int _level;
    private int _currentExp;
    private int _maxExp;
    private int _statPoint;
    private float _spreadReduction;

    private float _currentHp;
    public float MaxHp;
    private float _currentStamina;
    public float MaxStamina;
    private float _staminaRegenSpeed;

    private int _atk;
    private int _def;
    private int _shield;

    private bool _isDead;
    private int _coin;
    private float _currentWeight;
    private float _maxWeight;
    private bool _wasOverweight;

    //나중에 지역 구분할 때 넣으면 되는 변수
    //pubic int currentStagelevel;

    public GameData()
    {
        this._level = 1;
        this._currentExp =0;
        this._maxExp = 100;
        this._statPoint = 0;
        this._spreadReduction = 0f;

        this._currentHp = 100f;
        this.MaxHp = 100f;
        this._currentStamina = 100f;
        this.MaxStamina = 100f;
        this._staminaRegenSpeed = 20f;

        this._atk = 10;
        this._def = 0;
        this._shield = 0;

        this._isDead = false;
        this._coin = 0;
        this._currentWeight = 0f;
        this._maxWeight = 50f;
        this._wasOverweight = false;

        //this.currentStageLevel = 1;
    }
}
