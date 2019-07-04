﻿using System.Collections.Generic; // листы
using UnityEngine; // классы Юнити
using System.IO; // чтение-запись файлов
using UnityEngine.SceneManagement;

public struct GameStartSettings  {
    public byte chunkSize;
    public ChunkGenerationMode generationMode;
    public Difficulty difficulty;
    public static readonly GameStartSettings Empty;
    static GameStartSettings()
    {
        Empty = new GameStartSettings(ChunkGenerationMode.Standart, 16, Difficulty.Normal);
    }
    public GameStartSettings(ChunkGenerationMode i_genMode, byte i_chunkSize, Difficulty diff)
    {
        generationMode = i_genMode;
        chunkSize = i_chunkSize;
        difficulty = diff;
    }
    public GameStartSettings(ChunkGenerationMode i_genMode)
    {
        generationMode = i_genMode;
        chunkSize = 8;
        difficulty = Difficulty.Normal;
    }
    }

public enum Difficulty : byte {Utopia, Easy, Normal, Hard, Torture}
//dependencies:
// GameConstants.GetBlackoutStabilityTestHardness
// Lightning.CalculateDamage
// Monument.ArtifactStabilityTest
// RecruitingCenter.GetHireCost
//ScoreCalculator

public enum GameStart : byte {Nothing, Zeppelin, Headquarters}
public enum GameLevel : byte { Menu, Playable, Editor}
public enum GameEndingType : byte { Default, ColonyLost, TransportHubVictory, ConsumedByReal, ConsumedByLastSector}

/// -----------------------------------------------------------------------------

public sealed class GameMaster : MonoBehaviour
{
    public static GameMaster realMaster;
    public static float gameSpeed { get; private set; }
    public static bool sceneClearing { get; private set; }
    public static bool editMode = false, needTutorial = false;
    public static bool loading { get; private set; }
    public static bool loadingFailed; // hot
    public static bool soundEnabled { get; private set; }
    public static string savename { get; private set; }
    public static float LUCK_COEFFICIENT { get; private set; }
    public static float sellPriceCoefficient = 0.75f;
    public static byte layerCutHeight = 16, prevCutHeight = 16;

    public static Vector3 sceneCenter { get { return Vector3.one * Chunk.CHUNK_SIZE / 2f; } } // SCENE CENTER
    public static GameStartSettings gameStartSettings = GameStartSettings.Empty;    
    public static GeologyModule geologyModule;
    public static Audiomaster audiomaster;

    private static byte pauseRequests = 0;

    public Chunk mainChunk { get; private set; }
    public ColonyController colonyController { get; private set; }
    public EnvironmentMaster environmentMaster { get; private set; }
    public GlobalMap globalMap { get; private set; }
    public delegate void StructureUpdateHandler();
    public event StructureUpdateHandler labourUpdateEvent, lifepowerUpdateEvent;
    public GameStart startGameWith = GameStart.Zeppelin;

    public float lifeGrowCoefficient { get; private set; }
    public float demolitionLossesPercent { get; private set; }
    public float lifepowerLossesPercent { get; private set; }
    public float tradeVesselsTrafficCoefficient { get; private set; }
    public float upgradeDiscount { get; private set; }
    public float upgradeCostIncrease { get; private set; }
    public float warProximity { get; private set; } // 0 is far, 1 is nearby  
    public float gearsDegradeSpeed { get; private set; }
    public float stability { get; private set; }
    public Difficulty difficulty { get; private set; }
    //data
    private float timeGone, target_stability = 0.5f;
    public byte day { get; private set; }
    public byte month { get; private set; }
    public uint year { get; private set; }
    public const byte DAYS_IN_MONTH = 30, MONTHS_IN_YEAR = 12;
    public const float DAY_LONG = 60;
    // updating
    public const float LIFEPOWER_TICK = 1, LABOUR_TICK = 0.25f; // cannot be zero
    private float labourTimer = 0, lifepowerTimer = 0;
    private bool firstSet = true;
    // FOR TESTING
    public bool weNeedNoResources { get; private set; }
    public bool generateChunk = true;
    public byte test_size = 100;
    public bool _editMode = false;

    private static bool hotStart = false;
    private GameStartSettings hotStartSettings = new GameStartSettings(ChunkGenerationMode.GameLoading);
    private string hotStart_savename = "base";
    //

    #region static functions
    public static void SetSavename(string s)
    {
        savename = s;
    }
    public static void SetPause(bool pause)
    {
        if (pause)
        {
            pauseRequests++;
            Time.timeScale = 0;
            gameSpeed = 0;
        }
        else
        {
            if (pauseRequests > 0) pauseRequests--;
            if (pauseRequests == 0)
            {
                Time.timeScale = 1;
                gameSpeed = 1;
            }
        }
    }
    public static void ChangeScene(GameLevel level)
    {
        sceneClearing = true;
        SceneManager.LoadScene((int)level);
        sceneClearing = false;
        Structure.ResetToDefaults_Static();
    }
    public static void LoadingFail()
    {
        loadingFailed = true;
        SetPause(true);
        Debug.Log("loading failed");
    }
    #endregion

    public void ChangeModeToPlay()
    {
        if (!editMode) return;
        _editMode = false;
        Instantiate(Resources.Load<GameObject>("UIPrefs/UIController")).GetComponent<UIController>();
        firstSet = true;
        gameStartSettings.generationMode = ChunkGenerationMode.DontGenerate;
        startGameWith = GameStart.Zeppelin;
        Awake();
        Start();
    }

    private void Awake()
    {
        if (realMaster != null & realMaster != this)
        {
            Destroy(this);
            return;
        }
        realMaster = this;
        sceneClearing = false;
        if (PoolMaster.current == null)
        {
            PoolMaster pm = gameObject.AddComponent<PoolMaster>();
            pm.Load();
        }
        if (environmentMaster == null) environmentMaster = new GameObject("Environment master").AddComponent<EnvironmentMaster>();        
        if (!editMode)
        {
            if (globalMap == null) globalMap = gameObject.AddComponent<GlobalMap>();
            globalMap.Prepare();
            environmentMaster.Prepare();
        }        
    }

    void Start()
    {
        if (!firstSet) return;
        Time.timeScale = 1;
        gameSpeed = 1;
        pauseRequests = 0;
        audiomaster = gameObject.AddComponent<Audiomaster>();
        audiomaster.Prepare();

        //testzone
        editMode = _editMode;
        if (hotStart)
        {
            gameStartSettings = hotStartSettings;
            savename = hotStart_savename;
            hotStart = false;
        }
        //end of test data

        if (geologyModule == null) geologyModule = gameObject.AddComponent<GeologyModule>();
        if (!editMode)
        {
            lifeGrowCoefficient = 1;
            difficulty = gameStartSettings.difficulty;            
            //byte chunksize = gss.chunkSize;
            byte chunksize;
            chunksize = gameStartSettings.chunkSize;
            if (gameStartSettings.generationMode != ChunkGenerationMode.GameLoading)
            {
                if (gameStartSettings.generationMode != ChunkGenerationMode.DontGenerate)
                {
                    if (gameStartSettings.generationMode != ChunkGenerationMode.TerrainLoading)
                    {
                        Constructor.ConstructChunk(chunksize, gameStartSettings.generationMode);
                       // Constructor.ConstructBlock(chunksize);
                        if (gameStartSettings.generationMode == ChunkGenerationMode.Peak)
                        {
                            environmentMaster.PrepareIslandBasis(ChunkGenerationMode.Peak);
                        }
                    }
                    else LoadTerrain(SaveSystemUI.GetTerrainsPath() + '/' + savename + '.' + SaveSystemUI.TERRAIN_FNAME_EXTENSION);
                }
                FollowingCamera.main.ResetTouchRightBorder();
                FollowingCamera.main.CameraRotationBlock(false);

                switch (difficulty)
                {
                    case Difficulty.Utopia:
                        LUCK_COEFFICIENT = 1;
                        demolitionLossesPercent = 0;
                        lifepowerLossesPercent = 0;
                        sellPriceCoefficient = 1;
                        tradeVesselsTrafficCoefficient = 2;
                        upgradeDiscount = 0.5f; upgradeCostIncrease = 1.1f;
                        gearsDegradeSpeed = 0;
                        break;
                    case Difficulty.Easy:
                        LUCK_COEFFICIENT = 0.7f;
                        demolitionLossesPercent = 0.2f;
                        lifepowerLossesPercent = 0.1f;
                        sellPriceCoefficient = 0.9f;
                        tradeVesselsTrafficCoefficient = 1.5f;
                        upgradeDiscount = 0.3f; upgradeCostIncrease = 1.3f;
                        gearsDegradeSpeed = 0.00001f;
                        break;
                    case Difficulty.Normal:
                        LUCK_COEFFICIENT = 0.5f;
                        demolitionLossesPercent = 0.4f;
                        lifepowerLossesPercent = 0.3f;
                        sellPriceCoefficient = 0.75f;
                        tradeVesselsTrafficCoefficient = 1;
                        upgradeDiscount = 0.25f; upgradeCostIncrease = 1.5f;
                        gearsDegradeSpeed = 0.00002f;
                        break;
                    case Difficulty.Hard:
                        LUCK_COEFFICIENT = 0.1f;
                        demolitionLossesPercent = 0.7f;
                        lifepowerLossesPercent = 0.5f;
                        sellPriceCoefficient = 0.5f;
                        tradeVesselsTrafficCoefficient = 0.9f;
                        upgradeDiscount = 0.2f; upgradeCostIncrease = 1.7f;
                        gearsDegradeSpeed = 0.00003f;
                        break;
                    case Difficulty.Torture:
                        LUCK_COEFFICIENT = 0.01f;
                        demolitionLossesPercent = 1;
                        lifepowerLossesPercent = 0.85f;
                        sellPriceCoefficient = 0.33f;
                        tradeVesselsTrafficCoefficient = 0.75f;
                        upgradeDiscount = 0.1f; upgradeCostIncrease = 2f;
                        gearsDegradeSpeed = 0.00005f;
                        break;
                }
                warProximity = 0.01f;
                layerCutHeight = Chunk.CHUNK_SIZE; prevCutHeight = layerCutHeight;
                switch (startGameWith)
                {
                    case GameStart.Zeppelin:
                        Instantiate(Resources.Load<GameObject>("Prefs/Zeppelin"));
                        if (needTutorial)
                        {
                            GameLogUI.EnableDecisionWindow(null, Localization.GetTutorialHint(LocalizedTutorialHint.Landing));
                        }
                        else
                        {
                            GameLogUI.MakeAnnouncement(Localization.GetAnnouncementString(GameAnnouncements.SetLandingPoint));
                        }
                        break;

                    case GameStart.Headquarters:
                        List<SurfaceBlock> sblocks = mainChunk.surfaceBlocks;
                        SurfaceBlock sb = sblocks[Random.Range(0, sblocks.Count)];
                        int xpos = sb.pos.x;
                        int zpos = sb.pos.z;

                        //testzone
                        Structure s = HeadQuarters.GetHQ(1);
                       // weNeedNoResources = true;

                        //eo testzone                    
                        SurfaceBlock b = mainChunk.GetSurfaceBlock(xpos, zpos);
                        s.SetBasement(b, PixelPosByte.zero);


                        sb = mainChunk.GetSurfaceBlock(xpos - 1, zpos + 1);
                        if (sb == null)
                        {
                            sb = mainChunk.GetSurfaceBlock(xpos, zpos + 1);
                            if (sb == null)
                            {
                                sb = mainChunk.GetSurfaceBlock(xpos + 1, zpos + 1);
                                if (sb == null)
                                {
                                    sb = mainChunk.GetSurfaceBlock(xpos - 1, zpos);
                                    if (sb == null)
                                    {
                                        sb = mainChunk.GetSurfaceBlock(xpos + 1, zpos);
                                        if (sb == null)
                                        {
                                            sb = mainChunk.GetSurfaceBlock(xpos - 1, zpos - 1);
                                            if (sb == null)
                                            {
                                                sb = mainChunk.GetSurfaceBlock(xpos, zpos - 1);
                                                if (sb == null)
                                                {
                                                    sb = mainChunk.GetSurfaceBlock(xpos + 1, zpos - 1);
                                                    if (sb == null)
                                                    {
                                                        print("bad generation, do something!");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        StorageHouse firstStorage = Structure.GetStructureByID(Structure.STORAGE_0_ID) as StorageHouse;
                        firstStorage.SetBasement(sb, PixelPosByte.zero);
                        SetStartResources();
                        break;
                }
                FollowingCamera.main.WeNeedUpdate();
            }
            else LoadGame(SaveSystemUI.GetSavesPath() + '/' + savename + ".sav");
            if (savename == null | savename == string.Empty) savename = "autosave";
        }
        else
        {
            gameObject.AddComponent<PoolMaster>().Load();
            mainChunk = new GameObject("chunk").AddComponent<Chunk>();
            int size = Chunk.CHUNK_SIZE;
            int[,,] blocksArray = new int[size, size, size];
            size /= 2;
            blocksArray[size, size, size] = ResourceType.STONE_ID;
            mainChunk.CreateNewChunk(blocksArray);
        }

        stability = 0.5f;

        { // set look point
            FollowingCamera.camBasisTransform.position = sceneCenter;
        }
    }

    public void SetMainChunk(Chunk c) { mainChunk = c; }
    public void SetColonyController(ColonyController c)
    {
        colonyController = c;
    }

    #region updates
    private void Update()
    {
        if (loading) return;

        //testzone
       // if (Input.GetKeyDown("m") & colonyController != null) colonyController.AddEnergyCrystals(1000);
        //if (Input.GetKeyDown("n")) globalMap.ShowOnGUI();

        if (false && Input.GetKeyDown("o"))
        {
            var sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.OBSERVATORY_ID);
                s.SetBasement(sx, PixelPosByte.zero);
                (s as WorkBuilding).AddWorkers(50);
            }
            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.MINI_GRPH_REACTOR_3_ID);
                s.SetBasement(sx, PixelPosByte.zero);
            }
            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.MINI_GRPH_REACTOR_3_ID);
                s.SetBasement(sx, PixelPosByte.zero);
            }
            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.MINI_GRPH_REACTOR_3_ID);
                s.SetBasement(sx, PixelPosByte.zero);
            }
            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.QUANTUM_TRANSMITTER_4_ID);
                s.SetBasement(sx, PixelPosByte.zero);
            }

            //
            int l = Random.Range(10, 100);
            Artifact a;
            Artifact.AffectionType atype = Artifact.AffectionType.NoAffection;
            float f;
            for (int i = 0; i < l; i++)
            {
                f = Random.value;
                if (f < 0.25f) atype = Artifact.AffectionType.LifepowerAffection;
                else
                {
                    if (f > 0.5f)
                    {
                        if (f > 0.75f) atype = Artifact.AffectionType.SpaceAffection;
                        else atype = Artifact.AffectionType.StabilityAffection;
                    }
                }
                a = new Artifact(Random.value, Random.value, Random.value, atype);
                a.SetResearchStatus(true);
                a.Conservate();
            }
            //

            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.MONUMENT_ID);
                s.SetBasement(sx, PixelPosByte.zero);
               // UIController.current.Select(s);
            }

            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.ARTIFACTS_REPOSITORY_ID);
                s.SetBasement(sx, PixelPosByte.zero);
            }
            sx = mainChunk.GetRandomSurfaceBlock();

            
            Vector3Int ecpos = Vector3Int.zero;
            if (mainChunk.TryGetPlace(ref ecpos, SurfaceBlock.INNER_RESOLUTION))
            {
                Structure s = Structure.GetStructureByID(Structure.EXPEDITION_CORPUS_4_ID);
                s.SetBasement(mainChunk.surfaceBlocks[ecpos.z], PixelPosByte.zero);
            }
            if (mainChunk.TryGetPlace(ref ecpos, SurfaceBlock.INNER_RESOLUTION))
            {
                Structure s = Structure.GetStructureByID(Structure.RECRUITING_CENTER_4_ID);
                s.SetBasement(mainChunk.surfaceBlocks[ecpos.z], PixelPosByte.zero);
            }

            Crew c = Crew.CreateNewCrew(colonyController, 1f);
            sx = mainChunk.GetRandomSurfaceBlock();
            if (sx != null)
            {
                Structure s = Structure.GetStructureByID(Structure.SHUTTLE_HANGAR_4_ID);
                s.SetBasement(sx, PixelPosByte.zero);
                Shuttle sh = Instantiate(Resources.Load<GameObject>("Prefs/shuttle"), transform).GetComponent<Shuttle>();
                sh.FirstSet(s as Hangar);
                (s as Hangar).AssignShuttle(sh);
                c.SetShuttle(sh);
            }
            
        }
        //eo testzone

        float hc = 1f, gc = 0f;
        if (colonyController != null) {
            hc = colonyController.happiness_coefficient;
            if (colonyController.storage != null)
            {
                gc = colonyController.storage.standartResources[ResourceType.GRAPHONIUM_ID] / GameConstants.GRAPHONIUM_CRITICAL_MASS;
                gc *= 0.5f;
                gc = 1f - gc;
                // + блоки?
            }
        }
        float structureStabilizersEffect = 0f;
        target_stability = 0.25f * hc + 0.25f * gc + 0.25f * (1f - Mathf.Abs(globalMap.ascension - 0.5f)) + 0.25f * structureStabilizersEffect;
        if (stability != target_stability)
        {
            stability = Mathf.MoveTowards(stability, target_stability, GameConstants.STABILITY_CHANGE_SPEED * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (gameSpeed != 0)
        {
            float fixedTime = Time.fixedDeltaTime * gameSpeed;
            if (!editMode)
            {
                labourTimer -= fixedTime;
                lifepowerTimer -= fixedTime;
                if (labourTimer <= 0)
                {
                    labourTimer = LABOUR_TICK;
                    if (labourUpdateEvent != null) labourUpdateEvent();
                }
                if (lifepowerTimer <= 0)
                {
                    lifepowerTimer = LIFEPOWER_TICK;
                    Plant.PlantUpdate();
                    if (mainChunk != null) mainChunk.LifepowerUpdate(); // внутри обновляет все grasslands  
                    if (lifepowerUpdateEvent != null) lifepowerUpdateEvent();
                }
                timeGone += fixedTime;

                if (timeGone >= DAY_LONG)
                {
                    uint daysDelta = (uint)(timeGone / DAY_LONG);
                    if (daysDelta > 0 & colonyController != null)
                    {
                        // счет количества дней в ускорении отменен
                        colonyController.EverydayUpdate();
                    }
                    uint sum = day + daysDelta;
                    if (sum >= DAYS_IN_MONTH)
                    {
                        day = (byte)(sum % DAYS_IN_MONTH);
                        sum /= DAYS_IN_MONTH;
                        sum += month;
                        if (sum >= MONTHS_IN_YEAR)
                        {
                            month = (byte)(sum % MONTHS_IN_YEAR);
                            year = sum / MONTHS_IN_YEAR;
                        }
                        else month = (byte)sum;
                    }
                    else
                    {
                        day = (byte)sum;
                    }
                    timeGone = timeGone % DAY_LONG;
                }
            }
        }
    }
    #endregion

    #region game parameters
    public void SetStartResources()
    {
        //start resources
        switch (difficulty)
        {
            case Difficulty.Utopia:
                colonyController.AddCitizens(100);
                colonyController.storage.AddResource(ResourceType.metal_K, 100);
                colonyController.storage.AddResource(ResourceType.metal_M, 100);
                colonyController.storage.AddResource(ResourceType.metal_E, 50);
                colonyController.storage.AddResource(ResourceType.metal_N, 1);
                colonyController.storage.AddResource(ResourceType.Plastics, 200);
                colonyController.storage.AddResource(ResourceType.Food, 1000);                
                break;
            case Difficulty.Easy:
                colonyController.AddCitizens(70);
                colonyController.storage.AddResource(ResourceType.metal_K, 100);
                colonyController.storage.AddResource(ResourceType.metal_M, 60);
                colonyController.storage.AddResource(ResourceType.metal_E, 30);
                colonyController.storage.AddResource(ResourceType.Plastics, 150);
                colonyController.storage.AddResource(ResourceType.Food, 700);
                break;
            case Difficulty.Normal:
                colonyController.AddCitizens(50);
                colonyController.storage.AddResource(ResourceType.metal_K, 100);
                colonyController.storage.AddResource(ResourceType.metal_M, 50);
                colonyController.storage.AddResource(ResourceType.metal_E, 20);
                colonyController.storage.AddResource(ResourceType.Plastics, 100);
                colonyController.storage.AddResource(ResourceType.Food, 500);
                break;
            case Difficulty.Hard:
                colonyController.AddCitizens(40);
                colonyController.storage.AddResource(ResourceType.metal_K, 50);
                colonyController.storage.AddResource(ResourceType.metal_M, 20);
                colonyController.storage.AddResource(ResourceType.metal_E, 2);
                colonyController.storage.AddResource(ResourceType.Plastics, 10);
                colonyController.storage.AddResource(ResourceType.Food, 700);
                break;
            case Difficulty.Torture:
                colonyController.AddCitizens(30);
                colonyController.storage.AddResource(ResourceType.metal_K, 40);
                colonyController.storage.AddResource(ResourceType.metal_M, 20);
                colonyController.storage.AddResource(ResourceType.metal_E, 10);
                colonyController.storage.AddResource(ResourceType.Food, 750);
                break;
        }
        colonyController.storage.AddResources(ResourcesCost.GetCost(Structure.SETTLEMENT_CENTER_ID));
    }
    public float GetDifficultyCoefficient()
    {
        // 0 - 1 only!
        switch(difficulty)
        {
            case Difficulty.Utopia: return 0.1f;
            case Difficulty.Easy: return 0.25f;
            case Difficulty.Hard: return 0.7f;
            case Difficulty.Torture: return 1f;
            default: return 0.5f;
        }
    }
    #endregion
    //test
    public void OnGUI()
    {
        Rect r = new Rect(0, Screen.height - 16, 200, 16);
        GUI.Box(r, GUIContent.none);
        weNeedNoResources = GUI.Toggle(r, weNeedNoResources, "unlimited resources");
    }
    //
    public void GameOver(GameEndingType endType)
    {
        SetPause(true);
        UIController.current.FullDeactivation();

        double score = new ScoreCalculator().GetScore(this);
        Highscore.AddHighscore(new Highscore(colonyController.cityName, score, endType));

        string reason = Localization.GetEndingTitle(endType);
        switch (endType)
        {
            case GameEndingType.TransportHubVictory:
                {
                    Transform endpanel = Instantiate(Resources.Load<GameObject>("UIPrefs/endPanel"), UIController.current.mainCanvas).transform;
                    endpanel.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = reason;
                    endpanel.GetChild(2).GetComponent<UnityEngine.UI.Text>().text = Localization.GetWord(LocalizedWord.Score) + ": " + ((int)score).ToString();
                    var b = endpanel.GetChild(3).GetComponent<UnityEngine.UI.Button>();
                    b.onClick.AddListener(ReturnToMenuAfterGameOver);
                    b.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = Localization.GetWord(LocalizedWord.MainMenu);
                    b = endpanel.GetChild(4).GetComponent<UnityEngine.UI.Button>();
                    b.onClick.AddListener(() => { ContinueGameAfterEnd(endpanel.gameObject); });
                    b.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = Localization.GetWord(LocalizedWord.Continue);
                    break;
                }
            case GameEndingType.ColonyLost:
            case GameEndingType.Default:
            case GameEndingType.ConsumedByReal:
            case GameEndingType.ConsumedByLastSector:
            default:
                {
                    Transform failpanel = Instantiate(Resources.Load<GameObject>("UIPrefs/failPanel"), UIController.current.mainCanvas).transform;
                    failpanel.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = reason;
                    failpanel.GetChild(2).GetComponent<UnityEngine.UI.Text>().text = Localization.GetWord(LocalizedWord.Score) + ": " + ((int)score).ToString();
                    var b = failpanel.GetChild(3).GetComponent<UnityEngine.UI.Button>();
                    b.onClick.AddListener(ReturnToMenuAfterGameOver);
                    b.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = Localization.GetWord(LocalizedWord.MainMenu);
                    break;
                }
        }
    }
    public void ReturnToMenuAfterGameOver()
    {
        sceneClearing = true;
        ChangeScene(GameLevel.Menu);
        sceneClearing = false;
    }
    public void ContinueGameAfterEnd(GameObject panel)
    {
        Destroy(panel);
        UIController.current.FullReactivation();
        gameSpeed = 1;
    }

    public void OnApplicationQuit()
    {
        StopAllCoroutines();
        sceneClearing = true;
        Time.timeScale = 1;
        gameSpeed = 1;
        pauseRequests = 0;
    }

#region save-load system
    public bool SaveGame() { return SaveGame("autosave"); }
    public bool SaveGame(string name)
    { // заменить потом на persistent -  постоянный путь
        SetPause(true);

        string path = SaveSystemUI.GetSavesPath() + '/';
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        FileStream fs = File.Create(path + name + '.' + SaveSystemUI.SAVE_FNAME_EXTENSION);
        savename = name;
        //сразу передавать файловый поток для записи, чтобы не забивать озу
        #region gms mainPartFilling
        fs.Write(System.BitConverter.GetBytes(GameConstants.SAVE_SYSTEM_VERSION),0,4);
        // start writing
        fs.Write(System.BitConverter.GetBytes(gameSpeed), 0, 4);
        fs.Write(System.BitConverter.GetBytes(lifeGrowCoefficient), 0, 4);
        fs.Write(System.BitConverter.GetBytes(demolitionLossesPercent), 0, 4);
        fs.Write(System.BitConverter.GetBytes(lifepowerLossesPercent), 0, 4);
        fs.Write(System.BitConverter.GetBytes(LUCK_COEFFICIENT), 0, 4);
        fs.Write(System.BitConverter.GetBytes(sellPriceCoefficient), 0, 4);
        fs.Write(System.BitConverter.GetBytes(tradeVesselsTrafficCoefficient), 0, 4);
        fs.Write(System.BitConverter.GetBytes(upgradeDiscount), 0, 4);
        fs.Write(System.BitConverter.GetBytes(upgradeCostIncrease), 0, 4);
        fs.Write(System.BitConverter.GetBytes(warProximity), 0, 4); 
        //40
        fs.WriteByte((byte)difficulty);// 41
        fs.WriteByte((byte)startGameWith); // 42
        fs.WriteByte(prevCutHeight); //43
        fs.WriteByte(day); // 44
        fs.WriteByte(month); //45
        fs.Write(System.BitConverter.GetBytes(year), 0, 4);
        fs.Write(System.BitConverter.GetBytes(timeGone), 0, 4);
        fs.Write(System.BitConverter.GetBytes(gearsDegradeSpeed), 0, 4);
        fs.Write(System.BitConverter.GetBytes(stability), 0, 4);
        // 61
        fs.Write(System.BitConverter.GetBytes(labourTimer), 0, 4);
        fs.Write(System.BitConverter.GetBytes(lifepowerTimer), 0, 4);
        fs.Write(System.BitConverter.GetBytes(RecruitingCenter.GetHireCost()), 0, 4);
        // 73 - end
        #endregion

        fs.Write(System.BitConverter.GetBytes(Mission.nextID),0,4);
        globalMap.Save(fs);
        environmentMaster.Save(fs);
        Shuttle.SaveStaticData(fs);
        Artifact.SaveStaticData(fs);
        Crew.SaveStaticData(fs);
        mainChunk.SaveChunkData(fs);
        fs.Write(System.BitConverter.GetBytes(QuantumTransmitter.lastUsedID), 0, 4);
        colonyController.Save(fs); // <------- COLONY CONTROLLER
        Dock.SaveStaticDockData(fs);

        QuestUI.current.Save(fs);        
        Expedition.SaveStaticData(fs);        
        fs.Close();
        SetPause(false);
        return true;
    }
    public bool LoadGame() { return LoadGame("autosave"); }
    public bool LoadGame(string fullname)
    {  // отдельно функцию проверки и коррекции сейв-файла
        if (true) // <- тут будет функция проверки // када она будет?
        {
            SetPause(true);
            loading = true;
            // ОЧИСТКА
            StopAllCoroutines();
            if (Zeppelin.current != null)
            {
                Destroy(Zeppelin.current);
            }
            if (mainChunk != null) mainChunk.ClearChunk();
            // очистка подписчиков на ивенты невозможна, сами ивенты к этому моменту недоступны
            Crew.Reset(); Shuttle.Reset();
            Grassland.ScriptReset();
            Expedition.GameReset();
            Structure.ResetToDefaults_Static(); // все наследуемые resetToDefaults внутри
            if (colonyController != null) colonyController.ResetToDefaults(); // подчищает все списки
            else
            {
                colonyController = gameObject.AddComponent<ColonyController>();
                colonyController.Prepare();
            }
            //UI.current.Reset();


            // НАЧАЛО ЗАГРУЗКИ
            FileStream fs = File.Open(fullname, FileMode.Open);
            #region gms mainPartLoading
            var data = new byte[4];
            fs.Read(data, 0, 4);
            uint saveSystemVersion = System.BitConverter.ToUInt32(data, 0); // может пригодиться в дальнейшем
            //start writing
            data = new byte[73]; 
            fs.Read(data, 0, data.Length);
            gameSpeed = System.BitConverter.ToSingle(data, 0);
            lifeGrowCoefficient = System.BitConverter.ToSingle(data, 4);
            demolitionLossesPercent = System.BitConverter.ToSingle(data, 8);
            lifepowerLossesPercent = System.BitConverter.ToSingle(data, 12);
            LUCK_COEFFICIENT = System.BitConverter.ToSingle(data, 16);
            sellPriceCoefficient = System.BitConverter.ToSingle(data, 20);
            tradeVesselsTrafficCoefficient = System.BitConverter.ToSingle(data, 24);
            upgradeDiscount = System.BitConverter.ToSingle(data, 28);
            upgradeCostIncrease = System.BitConverter.ToSingle(data, 32);
            warProximity = System.BitConverter.ToSingle(data, 36);

            difficulty = (Difficulty)data[40];
            startGameWith = (GameStart)data[41];
            prevCutHeight = data[42];
            day = data[43];
            month = data[44];

            year = System.BitConverter.ToUInt32(data, 45);
            timeGone = System.BitConverter.ToSingle(data, 49);
            gearsDegradeSpeed = System.BitConverter.ToSingle(data, 53);
            stability = System.BitConverter.ToSingle(data, 57);

            labourTimer = System.BitConverter.ToSingle(data, 61);
            lifepowerTimer = System.BitConverter.ToSingle(data, 65);
            RecruitingCenter.SetHireCost(System.BitConverter.ToSingle(data, 69));
            #endregion

            data = new byte[4];
            fs.Read(data, 0, 4);
            Mission.SetNextIDValue(System.BitConverter.ToInt32(data,0));
            globalMap.Load(fs);
            if (loadingFailed) return false;

            if (environmentMaster == null) environmentMaster = gameObject.AddComponent<EnvironmentMaster>();
            environmentMaster.Load(fs);
            if (loadingFailed) return false;

            Shuttle.LoadStaticData(fs); // because of hangars
            if (loadingFailed) return false;

            Artifact.LoadStaticData(fs); // crews & monuments
            if (loadingFailed) return false;

            Crew.LoadStaticData(fs);
            if (loadingFailed) return false;

            if (mainChunk == null)
            {
                GameObject g = new GameObject("chunk");
                mainChunk = g.AddComponent<Chunk>();
            }
            mainChunk.LoadChunkData(fs);
            if (loadingFailed) return false;

            Settlement.TotalRecalculation(); // Totaru Annihirationu no imoto-chan
            if (loadingFailed) return false;

            fs.Read(data, 0, 4);
            QuantumTransmitter.SetLastUsedID(System.BitConverter.ToInt32(data, 0));

            colonyController.Load(fs); // < --- COLONY CONTROLLER
            if (loadingFailed) return false;

            Dock.LoadStaticData(fs);
            if (loadingFailed) return false;
            QuestUI.current.Load(fs);
            if (loadingFailed) return false;
            Expedition.LoadStaticData(fs);
            fs.Close();
            FollowingCamera.main.WeNeedUpdate();
            loading = false;
            savename = fullname;
            SetPause(false);
            return true;
        }
        else
        {
            GameLogUI.MakeAnnouncement(Localization.GetAnnouncementString(GameAnnouncements.LoadingFailed));
            return false;
        }
    }

    public bool SaveTerrain(string name)
    {
        string path = SaveSystemUI.GetTerrainsPath() + '/';
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        FileStream fs = File.Create(path + name + '.' + SaveSystemUI.TERRAIN_FNAME_EXTENSION);
        fs.Write(System.BitConverter.GetBytes(GameConstants.SAVE_SYSTEM_VERSION), 0, 4);
        mainChunk.SaveChunkData(fs);
        fs.Close();
        return true;
    }
    public bool LoadTerrain(string fullname)
    {
        FileStream fs = File.Open(fullname, FileMode.Open);
        var data = new byte[4];
        fs.Read(data, 0, 4);
        uint saveVersion = System.BitConverter.ToUInt32(data, 0);
        if (mainChunk == null)
        {
            GameObject g = new GameObject("chunk");
            mainChunk = g.AddComponent<Chunk>();
        }
        mainChunk.LoadChunkData(fs);
        fs.Close();
        FollowingCamera.main.WeNeedUpdate();
        return true;
    }
    #endregion

}
  
