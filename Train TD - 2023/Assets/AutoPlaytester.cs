using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class AutoPlaytester : MonoBehaviour {

    public static AutoPlaytester s;

    private void Awake() {
        if (s == null) {
            s = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Debug.Log("Constant Objects Singleton Reset");
        s = null;
    }


    public bool autoPlayerRunning = false;
    public bool runInBackground = false;

    private bool isFast = false;
    public void StartAutoPlayer(bool _isFast) {
        isFast = _isFast;
        if (!autoPlayerRunning) {
            StartCoroutine(AutoPlayTester());
        }
    }

    public InputActionReference stopAutoPlayer;

    private void OnEnable() {
        stopAutoPlayer.action.Enable();
    }

    private void OnDisable() {
        stopAutoPlayer.action.Disable();
        Application.runInBackground = runInBackground;
    }

    private void Update() {
        if (autoPlayerRunning) {
            if (stopAutoPlayer.action.WasPerformedThisFrame()) {
                StopAllCoroutines();
                autoPlayerRunning = false;
                TimeController.s.debugAlwaysFastForward = false;
                Application.runInBackground = runInBackground;
            }
        }
    }

    IEnumerator AutoPlayTester() {

        var startTime = Time.realtimeSinceStartup;
        runInBackground = Application.runInBackground;
        Application.runInBackground = true;
        print($"Start time {startTime}");
        var fastSpeed = 2f;
        if (isFast)
            fastSpeed = 12f;
        Time.timeScale = fastSpeed;
        TimeController.s.currentTimeScale = fastSpeed;
        TimeController.s.debugAlwaysFastForward = true;
        autoPlayerRunning = true;
        SettingsController.s.ResetRunAndReplayTutorial();
        
        print("Resetting and replaying tutorial");

        yield return null;
        yield return null;
        yield return new WaitUntil(() => !SceneLoader.s.isLoading);
        
        
        Time.timeScale = fastSpeed;
        TimeController.s.currentTimeScale = fastSpeed;
        TimeController.s.debugAlwaysFastForward = true;
        
        GameObject.Find("Start Game").GetComponent<Button>().onClick?.Invoke();

        print("starting game");
        
        yield return null;
        yield return null;
        yield return new WaitUntil(() => !PlayStateMaster.s.isLoading);

        var selectChar = GameObject.Find("Select Char");
        selectChar.GetComponent<Button>().onClick?.Invoke();
        
        print($"selecting char {selectChar.transform.parent.GetChild(0).GetComponent<TMP_Text>().text}");

        yield return null;
        yield return null;
        yield return new WaitUntil(() => !PlayStateMaster.s.isLoading);

        for (int i = 0; i < 3; i++) {
            PressMouseButton(MouseButton.Left);
            yield return null;
            yield return null;
        }

        yield return new WaitUntil(() => PlayerWorldInteractionController.s.canSelect);
        
        Keyboard keyboard = Keyboard.current;
        KeyboardState stateA = new KeyboardState();
        KeyboardState stateB = new KeyboardState();
        stateA.Press(Key.A);
        stateB.Release(Key.A);
     
        InputSystem.QueueStateEvent(keyboard, stateA);

        yield return new WaitForSeconds(1);
        InputSystem.QueueStateEvent(keyboard, stateB);
        
        PressKey(Key.W);
        PressKey(Key.S);
        PressKey(Key.D);
        
        PressKey(Key.R);
        yield return null;
        PressKey(Key.R);
        Scroll(200000f);
        
        print("Finished checking cam controls");
        
        
        yield return new WaitForSeconds(1);
        
        CameraController.s.ResetCameraPos();
        
        UpgradesController.s.SaveCartStateWithDelay();
        Train.s.SaveTrainState();
        
        print("Got destination cargo");

        yield return new WaitForSeconds(0.2f);

        var emptyCart = Train.s.carts[Train.s.carts.Count - 1];
        
        var fleaMarketCart = UpgradesController.s.shopCarts[0];

        for (int i = 0; i < UpgradesController.s.shopCarts.Count; i++) {
            var ammo = UpgradesController.s.shopCarts[i].GetComponentInChildren<ModuleAmmo>();
            if (ammo != null) {
                fleaMarketCart = UpgradesController.s.shopCarts[i];
            }
        }
        
        Assert.IsNotNull(fleaMarketCart.GetComponentInChildren<ModuleAmmo>());
        
        var fleaLocation = fleaMarketCart.GetComponentInParent<SnapLocation>();
        
        UpgradesController.s.RemoveCartFromShop(fleaMarketCart);
        Train.s.AddCartAtIndex(Train.s.carts.Count-1, fleaMarketCart);
        
        
        yield return new WaitForSeconds(0.2f);
        
        Train.s.RemoveCart(emptyCart);

        yield return new WaitForSeconds(0.2f);
        UpgradesController.s.SaveCartStateWithDelay();
        Train.s.SaveTrainState();
        
        print("Swapped empty cart around");
        
        yield return new WaitForSeconds(0.2f);
        
        ShopStateController.s.gateScript._OnMouseUpAsButton();
        
        print("Started combat");

        yield return new WaitForSeconds(1f);
        
        // ------------------------------------------------------------------------------------- COMBAT
        yield return CombatRoutine();
        // -------------------------------------------------------------------------------------


        yield return CombatRewardRoutine();
        

        for (int i = 0; i < DataHolder.s.buildings.Length; i++) {
            var building = DataHolder.s.buildings[i];
            /*if (!building.isMainEngine && !building.isMysteriousCart && !building.isCargo) {
                print($"Adding cart: {DataHolder.s.buildings[i].uniqueName}");
                var cart = Instantiate(DataHolder.s.buildings[i]);
                Train.s.AddCartAtIndex(Train.s.carts.Count - 1, cart);
                yield return new WaitForSeconds(1f);
            }*/
        }
        
        print("Finished adding every possible cart");

        yield return new WaitForSeconds(4f);
        
        /*for (int i = 0; i < DataHolder.s.artifacts.Length; i++) {
            print($"Adding Artifact: {DataHolder.s.artifacts[i].uniqueName}");
            var artifact = Instantiate(DataHolder.s.artifacts[i]);
            yield return new WaitForSeconds(0.5f);
            artifact.EquipArtifact();
            yield return new WaitForSeconds(0.5f);
        }
        
        print("Finished adding every possible artifact");*/
        
        yield return new WaitForSeconds(4f);
        
        UpgradesController.s.SaveCartStateWithDelay();
        Train.s.SaveTrainState();
        
        print("Got destination cargo");

        yield return new WaitForSeconds(0.2f);
        
        ShopStateController.s.gateScript._OnMouseUpAsButton();
        
        print("Started combat");
        
        /*// ------------------------------------------------------------------------------------- COMBAT
        yield return CombatRoutine();
        // -------------------------------------------------------------------------------------

        yield return CombatRewardRoutine(destinationCart);
        
        destinationCart = UpgradesController.s.leftCargo;
        UpgradesController.s.RemoveCartFromShop(destinationCart);
        Train.s.AddCartAtIndex(Train.s.carts.Count-1, destinationCart);
        
        UpgradesController.s.SnapDestinationCargos(destinationCart);
        UpgradesController.s.UpdateCartShopHighlights();
        UpgradesController.s.SaveCartStateWithDelay();
        Train.s.SaveTrainState();
        
        print("Got destination cargo");

        yield return new WaitForSeconds(0.2f);
        
        ShopStateController.s.gateScript._OnMouseUpAsButton();*/
        
        // don't repair. make sure we do die.
        
        print("Waiting to lose the game");

        yield return new WaitUntil(() => MissionLoseFinisher.s.isMissionLost);

        yield return null;
        
        print("Going back to menu");
        GameObject.Find("Lose back to menu").GetComponent<Button>().onClick?.Invoke();

        yield return new WaitUntil(() => !SceneLoader.s.isLoading);
        
        
        TimeController.s.currentTimeScale = 1;
        TimeController.s.debugAlwaysFastForward = false;
        autoPlayerRunning = false;
        Application.runInBackground = runInBackground;
        
        var seconds = Time.realtimeSinceStartup-startTime;
        
        print($"Auto Playtesting complete in {seconds}s. Check console for errors.");
    }
    
    


    private static Gamepad gamepad;
    IEnumerator CombatRoutine() {
        
        Assert.IsTrue(PlayStateMaster.s.isCombatInProgress());

        yield return new WaitForSeconds(1f);


        IDirectControllable directControllable = null;
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            directControllable = cart.GetComponentInChildren<IDirectControllable>();
            if (directControllable != null) {
                break;
            }
        }
        
        
        DirectControlMaster.s.AssumeDirectControl(directControllable);
        
        
        print("Assuming direct control");

        yield return new WaitForSeconds(4f);
        Mouse mouse = Mouse.current;
        MouseState stateClick = new MouseState();
        MouseState stateUnClick = new MouseState();
        stateClick = stateClick.WithButton(MouseButton.Left, true);
        stateUnClick = stateUnClick.WithButton(MouseButton.Left, false);
     
        InputSystem.QueueStateEvent(mouse, stateClick);

        print("Shooting");
        yield return new WaitForSeconds(1f);
        
        InputSystem.QueueStateEvent(mouse, stateUnClick);
        
        PressMouseButton(MouseButton.Right);
        
        print("Leaving direct control");

        yield return new WaitForSeconds(1f);

        if (gamepad != null) {
            InputSystem.AddDevice(gamepad);
        }
        
        if (gamepad == null) {
            gamepad = Gamepad.current;
            if(gamepad != null)
                InputSystem.AddDevice(gamepad);
        }
        
        if (gamepad == null) {
            gamepad = InputSystem.AddDevice<Gamepad>();
        }
        
        GamepadState gamepadStateClick = new GamepadState();
        GamepadState gamepadStateUnClick = new GamepadState();
        gamepadStateClick = gamepadStateClick.WithButton(GamepadButton.RightShoulder);
        gamepadStateUnClick = gamepadStateUnClick.WithButton(GamepadButton.RightShoulder, false);
     
        InputSystem.QueueStateEvent(gamepad, gamepadStateClick);
        InputSystem.QueueStateEvent(gamepad, gamepadStateUnClick);
        
        print("Changing direction");

        yield return new WaitForSeconds(1f);

        while (PlayStateMaster.s.isCombatInProgress()) {
            int repairCount = 0;
            int reloadCount = 0;
            for (int i = 0; i < Train.s.carts.Count; i++) {
                var cart = Train.s.carts[i];

                var healthModule = cart.GetHealthModule();
                var ammoModule = cart.GetComponentInChildren<ModuleAmmo>();
                while (healthModule.GetHealthPercent() < 0.75f) {
                    
                    repairCount += 1;
                    yield return new WaitForSeconds(0.1f);
                }

                if (ammoModule != null) {
                    while (ammoModule.curAmmo < 0.25f) {
                        reloadCount += 1;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }

            if (repairCount > 0)
                print($"Repaired {repairCount} times");
            if (reloadCount > 0)
                print($"Reloaded {reloadCount} times");
            yield return new WaitForSeconds(2);
        }

        InputSystem.RemoveDevice(gamepad);

        yield return new WaitForSeconds(2f);
    }


    IEnumerator CombatRewardRoutine() {
        GameObject.Find("Win Continue").GetComponent<Button>().onClick?.Invoke();
        var regularDeliveryLoc = GameObject.Find("Regular Delivery Snap Loc").GetComponent<SnapLocation>();

        /*Train.s.RemoveCart(destinationCart);
        UpgradesController.s.AddCartToShop(destinationCart, regularDeliveryLoc.myLocation);
        destinationCart.transform.SetParent(regularDeliveryLoc.snapTransform);
        */

        //Debug.Break();
        yield return new WaitForSeconds(10f);

        var rewardCart = regularDeliveryLoc.transform.parent.GetComponentInChildren<Cart>();
        UpgradesController.s.RemoveCartFromShop(rewardCart);
        Train.s.AddCartAtIndex(Train.s.carts.Count, rewardCart);

        var rewardArtifact = regularDeliveryLoc.transform.parent.GetComponentInChildren<Artifact>();
        Assert.IsNotNull(rewardArtifact);

        
        
        yield return new WaitForSeconds(2f);
        
        MissionWinFinisher.s.gateScript._OnMouseUpAsButton();
        
        yield return null;
        yield return null;
        yield return new WaitUntil(() => !PlayStateMaster.s.isLoading);
    }
    void PressKey(Key key) {
        Keyboard keyboard = Keyboard.current;
        KeyboardState stateA = new KeyboardState();
        KeyboardState stateB = new KeyboardState();
        stateA.Press(key);
        stateB.Release(key);
     
        InputSystem.QueueStateEvent(keyboard, stateA);
        InputSystem.QueueStateEvent(keyboard, stateB);
        
        print($"Pressed {key} key");
    }

    void PressMouseButton(MouseButton button) {
        Mouse mouse = Mouse.current;
        MouseState stateA = new MouseState();
        MouseState stateB = new MouseState();
        stateA = stateA.WithButton(button, true);
        stateB = stateB.WithButton(button, false);
     
        InputSystem.QueueStateEvent(mouse, stateA);
        InputSystem.QueueStateEvent(mouse, stateB);
        
        print($"Pressed {button} button");
    }
    
    void Scroll(float amount) {
        Mouse mouse = Mouse.current;
        MouseState stateA = new MouseState();
        stateA.scroll += new Vector2(0, amount);
     
        InputSystem.QueueStateEvent(mouse, stateA);
        
        print($"Scrolled {amount}");
    }

    /*IEnumerator _Scroll(float amount) {
        using (StateEvent.From(Mouse.current, out var eventPtr))
        {
            Mouse.current.scroll.WriteValueIntoEvent(amount, eventPtr);
            InputSystem.QueueDeltaStateEvent(Mouse.current, amount);
            //InputSystem.Update();
            print($"Scrolled {amount}");
        }

        yield return null;
        /*using (StateEvent.From(Keyboard.current, out var eventPtr))
        {
            Mouse.current.scroll.WriteValueIntoEvent(0f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            //InputSystem.Update();
        }#1#
    }*/

    void print(string message) {
        Debug.Log($"AUTOPLAYER: {message}");
    }
}
