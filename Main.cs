using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using static AnimatorSynchronizedDialog;
using static SteamInputWrapper;
using static WeaponManager.WeaponEntry;

namespace TestUnityInternal
{
    enum Tab { Aim, Visual, Misc }
    enum VisualSubTab { Players, Vehicles }

    class Main : MonoBehaviour
    {
     
        bool ravenCornerBox = false;
        bool eagleCornerBox = false;
        private bool ravenesp = false;
        private bool distance = false;
        private bool distancer = false;
        private bool ravenname = false;
        private bool eaglename = false;
        private bool eagleesp = false;
        private bool raventraceline = false;
        private bool eagletraceline = false;
        private bool ravenhealthw = false;
        private bool eaglehealthw = false;
        private bool vehicleesp = false;
        private bool vehiclename = false;
        private bool vehiclehealth = false;
        private bool vehicleespr = false;
        private bool vehiclenamer = false;
        private bool vehiclehealthr = false;

        private bool infammo = false;
        private bool aimbotarget = false;
        private bool silent = false;
        private bool instahit = false;
        private bool nosmoke = false;
        private bool norecoil = false;
        private bool rapidfire = false;
        private bool piercingbullet = false;
        private bool nospread = false;
        private bool godmode = false;
        private bool nofall = false;
        private bool vehiclegodmod = false;
        private bool multispeed = false;
        private bool multidamage = false;
        private bool crosshair = false;
        private bool drawfov = false;
        private bool aimbot = false;
        private bool enablePrediction = false;

        private float speedmulti = 1f;
        private float damagemulti = 50f;
        private float fovRadius = 100f;
        private float crossdimension = 50f;
        private float crossthick = 1f;
        private float aimLowerMeters = 0f;
        private bool disableCameraShake = false;

        private float aimLockSmooth = 10f;
        private Actor lockedTarget = null;
        private float targetIndicatorSize = 4f;
        private Color targetIndicatorColor = Color.red;
        private bool hasTargetIndicator = true;
        private Vector2 targetScreenPos;

        private List<Vehicle> allVehicles = new List<Vehicle>();
        private List<Actor> allActors = new List<Actor>();
        private List<Projectile> allProjectile = new List<Projectile>();
        private HashSet<int> processedProjectiles = new HashSet<int>();
        private PlayerFpParent playerCamParent;

        private Color ravenEspColor = Color.magenta;
        private Color vehiclenamecolor = Color.magenta;
        private Color vehiclenamercolor = Color.magenta;
        private Color ravennamecolor = Color.magenta;
        private Color eaglenamecolor = Color.magenta;
        private Color eagleEspColor = Color.cyan;
        private Color tracelineColor = Color.white;
        private Color discolor = Color.white;
        private Color discolorr = Color.white;
        private Color tracelineRColor = Color.white;
        private Color vehicleEspColor = Color.blue;
        private Color vehicleEspRColor = Color.yellow;
        private Color fovcolor = Color.white;

        public Transform target;
        private bool showMenu = false;
        private Rect menuRect = new Rect(40, 40, 720, 500);
        private Tab currentTab = Tab.Aim;
        private VisualSubTab currentVisualSub = VisualSubTab.Players;

        private float updateInterval = 0.5f;
        private float lastUpdateTime = 0f;

        private Projectile projectile_field;
        private Actor localplayer_field;
        private Hitbox hitbox_field;

        private bool showPalette = false;
        private Rect paletteRect;
        private System.Action<Color> onColorSelected;

        private readonly Color[] presetColors = new Color[] {
            Color.red, Color.green, Color.blue,
            Color.yellow, Color.cyan, Color.magenta,
            Color.white, Color.black, new Color(1f,0.5f,0f), new Color(0.5f,0f,1f)
        };

        private readonly Color bgDark = new Color(0.12f, 0.12f, 0.12f);
        private readonly Color panelDark = new Color(0.17f, 0.17f, 0.17f);
        private readonly Color panelBorder = new Color(0.22f, 0.22f, 0.22f);
        private readonly Color accentRed = new Color(0.78f, 0.16f, 0.16f);
        private readonly Color textLight = new Color(0.92f, 0.92f, 0.92f);

        private Texture2D texBg;
        private Texture2D texPanel;
        private Texture2D texAccent;
        private float lastVehicleBoxX, lastVehicleBoxY, lastVehicleBoxW, lastVehicleBoxH;

        public void Start()
        {
            
            projectile_field = FindObjectOfType<Projectile>();
            localplayer_field = FindObjectOfType<Actor>();
            hitbox_field = FindObjectOfType<Hitbox>();
            playerCamParent = FindObjectOfType<PlayerFpParent>();
            UpdateCachedObjects();

            texBg = CreateSolidTex(bgDark);
            texPanel = CreateSolidTex(panelDark);
            texAccent = CreateSolidTex(accentRed);
        }
        private void Aimbot()
        {
            Vector3 GetAccurateHeadPosition(Actor actor)
            {
                if (actor == null) return Vector3.zero;
                try
                {
                    var aiTargetField = actor.GetType().GetField("aiTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (aiTargetField != null)
                    {
                        var aiTarget = aiTargetField.GetValue(actor);
                        if (aiTarget != null)
                        {
                            var headField = aiTarget.GetType().GetField("head");
                            if (headField != null)
                            {
                                var head = headField.GetValue(aiTarget) as Transform;
                                if (head != null) return head.position;
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    var smr = actor.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null && smr.bones != null)
                    {
                        foreach (var bone in smr.bones)
                        {
                            if (bone != null && bone.name.ToLower().Contains("head"))
                                return bone.position;
                        }
                    }
                }
                catch { }

                return actor.transform.position + Vector3.up * 1.55f;
            }
            Vector3 PredictWithGravityAndIter(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVel, float bulletSpeed, float gravityScale)
            {
                const float GRAV = 9.81f;
                if (bulletSpeed < 0.001f) return targetPos;

                Vector3 d = targetPos - shooterPos;
                float a = Vector3.Dot(targetVel, targetVel) - bulletSpeed * bulletSpeed;
                float b = 2f * Vector3.Dot(d, targetVel);
                float c = Vector3.Dot(d, d);

                float t = 0f;
                bool solved = false;

                if (Mathf.Abs(a) < 1e-6f)
                {
                    if (Mathf.Abs(b) > 1e-6f)
                    {
                        float tlin = -c / b;
                        if (tlin > 0f) { t = tlin; solved = true; }
                    }
                }
                else
                {
                    float disc = b * b - 4f * a * c;
                    if (disc >= 0f)
                    {
                        float sqrtD = Mathf.Sqrt(disc);
                        float t1 = (-b + sqrtD) / (2f * a);
                        float t2 = (-b - sqrtD) / (2f * a);
                        float tt = float.MaxValue;
                        if (t1 > 0f && t1 < tt) tt = t1;
                        if (t2 > 0f && t2 < tt) tt = t2;
                        if (tt < float.MaxValue) { t = tt; solved = true; }
                    }
                }

                if (!solved)
                {
                    float dist = d.magnitude;
                    t = dist / bulletSpeed;
                    if (t < 0f) t = 0f;
                }

                Vector3 predicted = targetPos + targetVel * t;

                for (int it = 0; it < 4; it++)
                {
                    float drop = 0.5f * GRAV * gravityScale * t * t;
                    Vector3 aimPoint = predicted + new Vector3(0f, drop, 0f);
                    float newDist = Vector3.Distance(shooterPos, aimPoint);
                    float newT = newDist / bulletSpeed;
                    if (Mathf.Abs(newT - t) < 0.001f) break;
                    t = newT;
                    predicted = targetPos + targetVel * t;
                }

                float finalDrop = 0.5f * GRAV * gravityScale * t * t;
                return (targetPos + targetVel * t) + new Vector3(0f, finalDrop, 0f);
            }

            bool IsVisibleTo(Vector3 from, Actor targetActor, Vector3 targetPoint)
            {
                if (targetActor == null) return false;
                if (Physics.Linecast(from, targetPoint, out RaycastHit hit))
                {
                    if (hit.collider == null) return false;
                    var hitActor = hit.collider.GetComponentInParent<Actor>();
                    
                    return hitActor == targetActor;
                }
                
                return true;
            }

            if (Input.GetMouseButton(1))
            {
                if (playerCamParent == null)
                    playerCamParent = FindObjectOfType<PlayerFpParent>();
                Vector3 shooterPos = (playerCamParent != null && playerCamParent.fpCamera != null)
                    ? playerCamParent.fpCamera.transform.position
                    : Camera.main.transform.position;

                Actor bestTarget = null;
                float bestWorldDist = float.MaxValue;
                float bestScreenDist = float.MaxValue;

                float bulletSpeed = 250f;
                float gravityScale = 1f;
                try
                {
                    var fpsActor = (playerCamParent != null) ? playerCamParent.GetComponentInParent<FpsActorController>() : null;
                    var weapon = fpsActor != null ? fpsActor.actor.activeWeapon : null;
                    if (weapon != null)
                    {
                        var prjField = weapon.GetType().GetField("prj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var projectileField = weapon.GetType().GetField("projectile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        object prjObj = prjField != null ? prjField.GetValue(weapon) : (projectileField != null ? projectileField.GetValue(weapon) : null);
                        if (prjObj != null)
                        {
                            var velField = prjObj.GetType().GetField("velocity");
                            var velProp = prjObj.GetType().GetProperty("velocity");
                            Vector3 vel = Vector3.zero;
                            if (velField != null) vel = (Vector3)velField.GetValue(prjObj);
                            else if (velProp != null) vel = (Vector3)velProp.GetValue(prjObj, null);
                            if (vel.magnitude > 0.1f) bulletSpeed = vel.magnitude;

                            var gravField = prjObj.GetType().GetField("gravityMultiplier");
                            var gravProp = prjObj.GetType().GetProperty("gravityMultiplier");
                            if (gravField != null) gravityScale = (float)gravField.GetValue(prjObj);
                            else if (gravProp != null) gravityScale = (float)gravProp.GetValue(prjObj, null);
                        }
                    }
                }
                catch { }

                foreach (Actor enemy in allActors)
                {
                    if (enemy == null || enemy.dead || enemy.team != 1) continue;

                    Vector3 headPos = GetAccurateHeadPosition(enemy);
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);
                    if (screenPos.z <= 0f) continue;

                    float screenDist = Vector2.Distance(
                        new Vector2(screenPos.x, Screen.height - screenPos.y),
                        new Vector2(Screen.width / 2f, Screen.height / 2f)
                    );
                    if (screenDist > fovRadius) continue;
                    if (!IsVisibleTo(shooterPos, enemy, headPos)) continue;

                    float worldDist = Vector3.Distance(shooterPos, headPos);
                    if (worldDist < bestWorldDist || (Mathf.Approximately(worldDist, bestWorldDist) && screenDist < bestScreenDist))
                    {
                        bestWorldDist = worldDist;
                        bestScreenDist = screenDist;
                        bestTarget = enemy;
                    }
                }

                if (bestTarget != null)
                    lockedTarget = bestTarget;

                if (lockedTarget != null)
                {
                    Vector3 headPos = GetAccurateHeadPosition(lockedTarget);
                    Vector3 predictedPos = headPos;
                    if (enablePrediction)
                    {
                        predictedPos = PredictWithGravityAndIter(shooterPos, headPos, lockedTarget.Velocity(), bulletSpeed, gravityScale);
                    }
                    if (aimLowerMeters > 0f)
                        predictedPos.y -= aimLowerMeters;

                    Vector3 scr = Camera.main.WorldToScreenPoint(predictedPos);
                    hasTargetIndicator = scr.z > 0f;
                    if (hasTargetIndicator)
                        targetScreenPos = new Vector2(scr.x, Screen.height - scr.y);
                    try
                    {
                        Transform camRoot = playerCamParent != null ? playerCamParent.fpCameraRoot : Camera.main.transform;
                        Vector3 dir = (predictedPos - shooterPos).normalized;
                        Quaternion targetRot = Quaternion.LookRotation(dir);
                        camRoot.rotation = Quaternion.Slerp(camRoot.rotation, targetRot, Time.deltaTime * aimLockSmooth);
                        if (playerCamParent != null && playerCamParent.fpCamera != null)
                        {
                            playerCamParent.fpCamera.transform.rotation = Quaternion.Slerp(
                                playerCamParent.fpCamera.transform.rotation,
                                targetRot,
                                Time.deltaTime * aimLockSmooth
                            );
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Lock-on rotation error: " + e.Message);
                    }
                }
            }
            else
            {
                lockedTarget = null;
                hasTargetIndicator = false;
            }
        }
        private void SilentAim()
        {
            if (allProjectile == null || allProjectile.Count == 0) return;
            if (playerCamParent == null) playerCamParent = FindObjectOfType<PlayerFpParent>();

            Vector3 GetAccurateHeadPosition(Actor actor)
            {
                if (actor == null) return Vector3.zero;
                try
                {
                    var aiTargetField = actor.GetType().GetField("aiTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (aiTargetField != null)
                    {
                        var aiTarget = aiTargetField.GetValue(actor);
                        if (aiTarget != null)
                        {
                            var headField = aiTarget.GetType().GetField("head");
                            if (headField != null)
                            {
                                var head = headField.GetValue(aiTarget) as Transform;
                                if (head != null) return head.position;
                            }
                        }
                    }
                }
                catch { }

                var smr = actor.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && smr.bones != null)
                {
                    foreach (var bone in smr.bones)
                    {
                        if (bone != null && bone.name.ToLower().Contains("head"))
                            return bone.position;
                    }
                }

                return actor.transform.position + Vector3.up * 1.55f;
            }

            Actor bestTarget = null;
            float bestScreenDist = float.MaxValue;

            foreach (Actor enemy in allActors)
            {
                if (enemy == null || enemy.dead || enemy.team != 1) continue;
                Vector3 headPos = GetAccurateHeadPosition(enemy);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);
                if (screenPos.z <= 0f) continue;

                float screenDist = Vector2.Distance(
                    new Vector2(screenPos.x, Screen.height - screenPos.y),
                    new Vector2(Screen.width / 2f, Screen.height / 2f)
                );

                if (screenDist < fovRadius && screenDist < bestScreenDist)
                {
                    bestScreenDist = screenDist;
                    bestTarget = enemy;
                }
            }

            if (bestTarget == null) return;
            Vector3 targetHead = GetAccurateHeadPosition(bestTarget) - new Vector3(0f, aimLowerMeters, 0f);

            foreach (Projectile prj in allProjectile)
            {
                if (prj == null || !prj.firedByPlayer) continue;
                Rigidbody rb = prj.GetComponent<Rigidbody>();
                if (rb == null && prj.transform.TryGetComponent(out Rigidbody foundRb))
                    rb = foundRb;
                Vector3 dir = (targetHead - prj.transform.position).normalized;
                float speed = 0f;
                if (prj.configuration != null && prj.configuration.speed > 1f)
                    speed = prj.configuration.speed;
                else if (rb != null)
                    speed = rb.velocity.magnitude;
                else
                    speed = prj.velocity.magnitude;
                Vector3 newVel = dir * speed;

                if (rb != null)
                {
                    rb.velocity = newVel;
                    rb.angularVelocity = Vector3.zero; 
                }

                prj.velocity = newVel; 
                prj.transform.position += dir * Time.deltaTime * speed * 0.4f;
            }
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                showMenu = !showMenu;

            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdateCachedObjects();
                lastUpdateTime = Time.time;
            }

            if (silent)
            {
                SilentAim();
            }

            if (aimbot)
            {
                Aimbot();
            }
            if (disableCameraShake)
            {
                try
                {
                    if (playerCamParent == null)
                        playerCamParent = FindObjectOfType<PlayerFpParent>();

                    if (playerCamParent != null)
                    {
                        var ppType = typeof(PlayerFpParent);
                        
                        var shakeField = ppType.GetField("screenshake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (shakeField != null)
                            shakeField.SetValue(playerCamParent, false);

                        var coField = ppType.GetField("screenshakeCoroutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (coField != null)
                        {
                            var coroutineObj = coField.GetValue(playerCamParent) as Coroutine;
                            if (coroutineObj != null)
                            {
                                playerCamParent.StopCoroutine(coroutineObj);
                                coField.SetValue(playerCamParent, null);
                            }
                        }
                        var resetMethod = ppType.GetMethod("ResetRecoil", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (resetMethod != null)
                            resetMethod.Invoke(playerCamParent, null);
                        var resetCamMethod = ppType.GetMethod("ResetCameraOffset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (resetCamMethod != null)
                            resetCamMethod.Invoke(playerCamParent, null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("DisableCameraShake reflection error: " + e.Message);
                }
            }

        }
        void UpdateCachedObjects()
        {
            allVehicles.Clear();
            allVehicles.AddRange(FindObjectsOfType<Vehicle>());
            allActors.Clear();
            allActors.AddRange(FindObjectsOfType<Actor>());
            allProjectile.Clear();
            allProjectile.AddRange(FindObjectsOfType<Projectile>());
        }
        public void OnGUI()
        {
            GUI.contentColor = textLight;

            if (showMenu)
                menuRect = GUI.Window(0, menuRect, DrawMenu, "");

            if (showPalette)
                GUI.Window(1, paletteRect, DrawPalette, "Choose Color");

            if (drawfov)
                Render.DrawCircle(new Vector2(Screen.width / 2f, Screen.height / 2f), fovRadius, fovcolor, 32, 2f);

            if (crosshair)
                Render.DrawCenteredLines(crossdimension, Color.red, crossthick);

            if (hasTargetIndicator && aimbotarget)
            {
                Render.DrawCircle(targetScreenPos, targetIndicatorSize, targetIndicatorColor, 32, 2f);
            }

            DrawWatermark();

            foreach (Projectile prj in allProjectile)
            {
                if (prj == null) continue;

                if (prj.firedByPlayer && instahit == true)
                {
                    if (prj.configuration != null)
                    {
                        prj.configuration.gravityMultiplier = 0f;
                        prj.configuration.speed = 999f;
                    }
                }

                if (piercingbullet)
                {
                    if (prj.firedByPlayer)
                    {
                        prj.configuration.piercing = true;

                    }
                }

                if (multidamage)
                {
                    if (prj.firedByPlayer)
                    {
                        if (prj.configuration != null)
                            prj.configuration.damage = damagemulti;
                    }
                }
            }

            foreach (Vehicle v in allVehicles)
            {
                if (v == null) continue;

                Vector3 foot = Camera.main.WorldToScreenPoint(v.transform.position);
                Vector3 head = Camera.main.WorldToScreenPoint(v.transform.position + Vector3.up * 2f);

                if (vehiclegodmod && foot.z > 0f && !v.dead && v.claimedByPlayer)
                {
                    v.maxHealth = 9999f;
                    v.health = 9999f;
                }

                if (vehicleesp && foot.z > 0f && !v.dead && v.ownerTeam == 0)
                {
                    DrawVehicleBoxESP(foot, head, vehicleEspColor);

                }

                if (vehicleespr && foot.z > 0f && !v.dead && v.ownerTeam == 1)
                {
                    DrawVehicleBoxESP(foot, head, vehicleEspRColor);
                }

                if (vehiclename && foot.z > 0 && !v.dead && v.ownerTeam == 0)
                {
                    float h = (head.y - foot.y) * 0.8f;
                    float w = h * 1.4f;
                    Vector2 labelPos = new Vector2(foot.x, Screen.height - head.y - 8f);
                    Render.DrawString(labelPos, v.name, vehiclenamecolor, true);
                }

                if (vehiclenamer && foot.z > 0 && !v.dead && v.ownerTeam == 1)
                {
                    float h = (head.y - foot.y) * 0.8f;
                    float w = h * 1.4f;
                    Vector2 labelPos = new Vector2(foot.x, Screen.height - head.y - 8f);
                    Render.DrawString(labelPos, v.name, vehiclenamercolor, true);
                }

                if (vehiclehealth && head.z > 0f && !v.dead && v.ownerTeam == 0)
                {
                    float healthPerc = Mathf.Clamp01(v.health / v.maxHealth);
                    float filledHeight = lastVehicleBoxH * healthPerc;
                    float barWidth = 4f;
                    float x = lastVehicleBoxX - barWidth - 2f;
                    float y = lastVehicleBoxY;

                    Render.DrawBox(x, y, barWidth, lastVehicleBoxH, Color.black, 1f);
                    Render.DrawBox(x, y + (lastVehicleBoxH - filledHeight), barWidth, filledHeight, Color.green, 1f);
                }

                if (vehiclehealthr && head.z > 0f && !v.dead && v.ownerTeam == 1)
                {
                    float healthPerc = Mathf.Clamp01(v.health / v.maxHealth);
                    float filledHeight = lastVehicleBoxH * healthPerc;
                    float barWidth = 4f;
                    float x = lastVehicleBoxX - barWidth - 2f;
                    float y = lastVehicleBoxY;

                    Render.DrawBox(x, y, barWidth, lastVehicleBoxH, Color.black, 1f);
                    Render.DrawBox(x, y + (lastVehicleBoxH - filledHeight), barWidth, filledHeight, Color.green, 1f);
                }
            }

            foreach (Actor player in allActors)
            {
                if (player == null) continue;

                Vector3 foot = Camera.main.WorldToScreenPoint(player.transform.position);
                Vector3 head = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.up * 2f);

                if (multispeed == true && player != null && player.aiControlled == false)
                {
                    player.speedMultiplier = speedmulti;
                }

                if (nosmoke == true && player != null && player.aiControlled == false)
                {
                    player.hasSmokeScreen = false;
                }

                if (godmode && player != null && player.aiControlled == false)
                {
                    player.maxHealth = 9999f;
                    player.health = 9999f;
                }

                if (nofall && player != null && player.aiControlled == false)
                {
                    player.maxBalance = 9999f;
                    player.balance = 9999f;
                }

                if (infammo && player != null && player.aiControlled == false)
                {
                    if (player.activeWeapon != null)
                        player.activeWeapon.ammo = 999;
                }

                if (norecoil && player.aiControlled == false)
                {
                    if (player.activeWeapon != null && player.activeWeapon.configuration != null)
                        player.activeWeapon.configuration.kickback = 0f;
                }

                if (rapidfire && player.aiControlled == false)
                {
                    if (player.activeWeapon != null && player.activeWeapon.configuration != null)
                        player.activeWeapon.configuration.cooldown = 0.005f;
                }

                if (nospread && player.aiControlled == false)
                {
                    if (player.activeWeapon != null && player.activeWeapon.configuration != null)
                        player.activeWeapon.configuration.spread = 0f;
                }

                if (!player.seat)
                {
                    if (ravenesp && player.team == 1 && foot.z > 0f && !player.dead)
                    {
                        float h = head.y - foot.y;
                        float w = h / 2f;
                        float x = foot.x - (w / 2);
                        float y = Screen.height - foot.y - h;

                        if (ravenCornerBox)
                            DrawCornerBox(x, y, w, h, ravenEspColor);
                        else
                            DrawBoxESP(foot, head, ravenEspColor);

                        if (distancer)
                        {
                            float dist = Vector3.Distance(Camera.main.transform.position, player.transform.position);
                            float xCenter = foot.x;
                            float yBottom = Screen.height - foot.y + 12f;
                            string text = string.Format("[{0:F0}]", dist);
                            Render.DrawString(new Vector2(xCenter, yBottom), text, Color.white, true);
                        }
                    }

                    if (eagleesp && player.team == 0 && foot.z > 0f && !player.dead)
                    {
                        float h = head.y - foot.y;
                        float w = h / 2f;
                        float x = foot.x - (w / 2);
                        float y = Screen.height - foot.y - h;

                        if (eagleCornerBox)
                            DrawCornerBox(x, y, w, h, eagleEspColor);
                        else
                            DrawBoxESP(foot, head, eagleEspColor);

                        if (distance)
                        {
                            float dist = Vector3.Distance(Camera.main.transform.position, player.transform.position);
                            float xCenter = foot.x;
                            float yBottom = Screen.height - foot.y + 12f;
                            string text = string.Format("[{0:F0}]", dist);
                            Render.DrawString(new Vector2(xCenter, yBottom), text, Color.white, true);
                        }
                    }

                    if (raventraceline && player.team == 1 && foot.z > 0f && !player.dead)
                        DrawTraceline(foot, head, tracelineRColor);

                    if (eagletraceline && player.team == 0 && foot.z > 0f && !player.dead)
                        DrawTraceline(foot, head, tracelineColor);

                    if (ravenname && player.team == 1 && foot.z > 0 && !player.dead)
                    {
                        float h = (head.y - foot.y) * 0.8f;
                        float w = h * 1.4f;
                        Vector2 labelPos = new Vector2(foot.x, Screen.height - head.y - 8f);
                        Render.DrawString(labelPos, player.name, ravennamecolor, true);
                    }

                    if (eaglename && player.team == 0 && foot.z > 0 && !player.dead)
                    {
                        float h = (head.y - foot.y) * 0.8f;
                        float w = h * 1.4f;
                        Vector2 labelPos = new Vector2(foot.x, Screen.height - head.y - 8f);
                        Render.DrawString(labelPos, player.name, eaglenamecolor, true);
                    }

                    if ((ravenhealthw && player.team == 1) || (eaglehealthw && player.team == 0))
                    {
                        if (head.z > 0f && !player.dead)
                        {
                            float healthPerc = player.health / player.maxHealth;
                            float barHeight = foot.y - head.y;
                            float barWidth = 2f;
                            float boxWidth = GetBoxESPWidth(foot, head);
                            float yStart = Screen.height - foot.y;

                            Rect bg = new Rect(foot.x - boxWidth / 2 - barWidth - 2f, yStart, barWidth, barHeight);
                            Render.DrawBox(bg.x, bg.y, bg.width, bg.height, Color.black, 1f);

                            Rect fill = new Rect(bg.x, yStart, barWidth, barHeight * healthPerc);
                            Render.DrawBox(fill.x, fill.y, fill.width, fill.height, Color.green, 1f);
                        }
                    }
                }
            }
            void DrawWatermark()
            {
                float wmWidth = 120f;
                float wmHeight = 26f;
                float x = 10f;
                float y = 10f;

                GUI.DrawTexture(new Rect(x, y, wmWidth, wmHeight), texPanel);
                GUI.DrawTexture(new Rect(x, y, 4f, wmHeight), texAccent);

                GUIStyle wmStyle = new GUIStyle(GUI.skin.label);
                wmStyle.fontSize = 12;
                wmStyle.normal.textColor = textLight;
                wmStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(x + 4f, y, wmWidth - 4f, wmHeight), "Buck3ts41", wmStyle);
            }

        }
        void DrawMenu(int id)
        {
            float w = menuRect.width;
            float h = menuRect.height;

            GUI.DrawTexture(new Rect(0, 0, w, h), texBg);
            GUI.DrawTexture(new Rect(0, 0, w, 4), texAccent);
            float sidebarW = 140f;
            GUI.DrawTexture(new Rect(8, 12, sidebarW, h - 24), texPanel);
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.alignment = TextAnchor.MiddleLeft;
            tabStyle.padding = new RectOffset(12, 6, 6, 6);
            tabStyle.normal.textColor = textLight;
            tabStyle.fontSize = 12;

            Rect[] tabs = new Rect[] {
                new Rect(18, 60, sidebarW - 28, 40),
                new Rect(18, 110, sidebarW - 28, 40),
                new Rect(18, 160, sidebarW - 28, 40)
            };

            if (currentTab == Tab.Aim) GUI.DrawTexture(new Rect(tabs[0].x - 4, tabs[0].y, 4, tabs[0].height), texAccent);
            if (GUI.Button(tabs[0], "  Aim", tabStyle)) currentTab = Tab.Aim;

            if (currentTab == Tab.Visual) GUI.DrawTexture(new Rect(tabs[1].x - 4, tabs[1].y, 4, tabs[1].height), texAccent);
            if (GUI.Button(tabs[1], "  Visual", tabStyle)) { currentTab = Tab.Visual; currentVisualSub = VisualSubTab.Players; }

            if (currentTab == Tab.Misc) GUI.DrawTexture(new Rect(tabs[2].x - 4, tabs[2].y, 4, tabs[2].height), texAccent);
            if (GUI.Button(tabs[2], "  Misc", tabStyle)) currentTab = Tab.Misc;

            GUILayout.BeginArea(new Rect(sidebarW + 20, 20, w - sidebarW - 36, h - 36));

            switch (currentTab)
            {
                case Tab.Aim:
                    DrawAimTab();
                    break;
                case Tab.Visual:
                    DrawVisualTab();
                    break;
                case Tab.Misc:
                    DrawMiscTab();
                    break;
            }

            GUILayout.EndArea();
            GUI.DragWindow(new Rect(0, 0, w, 24));
        }
        void DrawAimTab()
        {
            GUIStyle label = new GUIStyle(GUI.skin.label);
            label.fontSize = 12;
            label.normal.textColor = textLight;

            GUIStyle header = new GUIStyle(GUI.skin.label);
            header.fontStyle = FontStyle.Bold;
            header.fontSize = 13;
            header.normal.textColor = textLight;

            GUILayout.BeginVertical("box");
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texPanel);
            GUILayout.Label("Aimbot", header);

            GUILayout.BeginVertical("box");
            aimbot = GUILayout.Toggle(aimbot, " Aimbot");
            silent = GUILayout.Toggle(silent, " Silent Aim");
            aimbotarget = GUILayout.Toggle(aimbotarget, " Show Prediction");

            GUILayout.Space(6);
            enablePrediction = GUILayout.Toggle(enablePrediction, " Enable Prediction");

            GUILayout.Space(6);
            GUILayout.Label("Smoothness", label);

            aimLockSmooth = GUILayout.HorizontalSlider(aimLockSmooth, 1f, 20f);
            
            GUILayout.Space(6);
            GUILayout.Label("Aimbot Y Adjustment", label);
            aimLowerMeters = GUILayout.HorizontalSlider(aimLowerMeters, 0f, 1.5f);

            GUILayout.Space(6);
            GUILayout.Space(8);
            drawfov = GUILayout.Toggle(drawfov, " Draw FOV");
            if (drawfov)
            {
                GUILayout.Label("FOV Radius: " + fovRadius.ToString("F0"), label);
                fovRadius = GUILayout.HorizontalSlider(fovRadius, 0f, 500f);
            }

            GUILayout.Space(8);
            crosshair = GUILayout.Toggle(crosshair, " Crosshair");
            if (crosshair)
            {
                GUILayout.Label("Crosshair Size", label);
                crossdimension = GUILayout.HorizontalSlider(crossdimension, 0f, 200f);
                GUILayout.Label("Crosshair Thickness", label);
                crossthick = GUILayout.HorizontalSlider(crossthick, 0f, 5f);
            }

            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }
        void DrawVisualTab()
        {
            GUIStyle header = new GUIStyle(GUI.skin.label);
            header.fontStyle = FontStyle.Bold;
            header.fontSize = 13;
            header.normal.textColor = textLight;

            GUILayout.BeginVertical();
            DrawPlayerESP();
            GUILayout.EndVertical();
        }
        void DrawPlayerESP()
        {
            GUIStyle header = new GUIStyle(GUI.skin.label);
            header.fontStyle = FontStyle.Bold;
            header.fontSize = 13;
            header.normal.textColor = textLight;

            GUILayout.BeginHorizontal();

            // Raven 
            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Raven ESP", header);
            ravenesp = ColorToggle(ravenesp, ravenEspColor, " Box", c => ravenEspColor = c);
            ravenCornerBox = GUILayout.Toggle(ravenCornerBox, " Use Corner Box");
            ravenname = ColorToggle(ravenname, ravennamecolor, " Name", c => ravennamecolor = c);
            distancer = ColorToggle(distancer, discolorr, " Distance", c => discolorr = c);
            raventraceline = ColorToggle(raventraceline, tracelineRColor, " Traceline", c => tracelineRColor = c);
            ravenhealthw = GUILayout.Toggle(ravenhealthw, " Health Bar");
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            // Eagle
            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Eagle ESP", header);
            eagleesp = ColorToggle(eagleesp, eagleEspColor, " Box", c => eagleEspColor = c);
            eagleCornerBox = GUILayout.Toggle(eagleCornerBox, " Use Corner Box");
            eaglename = ColorToggle(eaglename, eaglenamecolor, " Name", c => eaglenamecolor = c);
            distance = ColorToggle(distance, discolor, " Distance", c => discolor = c);
            eagletraceline = ColorToggle(eagletraceline, tracelineColor, " Traceline", c => tracelineColor = c);
            eaglehealthw = GUILayout.Toggle(eaglehealthw, " Health Bar");
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Vehicles 
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Vehicles", header);
            vehicleespr = ColorToggle(vehicleespr, vehicleEspRColor, " Raven Vehicle", c => vehicleEspRColor = c);
            vehiclenamer = ColorToggle(vehiclenamer, vehiclenamercolor, " Name (Raven)", c => vehiclenamercolor = c);
            vehiclehealthr = GUILayout.Toggle(vehiclehealthr, " Health Bar (Raven)");

            GUILayout.Space(4);
            vehicleesp = ColorToggle(vehicleesp, vehicleEspColor, " Eagle Vehicle", c => vehicleEspColor = c);
            vehiclename = ColorToggle(vehiclename, vehiclenamecolor, " Name (Eagle)", c => vehiclenamecolor = c);
            vehiclehealth = GUILayout.Toggle(vehiclehealth, " Health Bar (Eagle)");
            GUILayout.EndVertical();
        }
        void DrawMiscTab()
        {
            GUIStyle header = new GUIStyle(GUI.skin.label);
            header.fontStyle = FontStyle.Bold;
            header.fontSize = 13;
            header.normal.textColor = textLight;

            GUILayout.BeginVertical("box");
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texPanel);
            GUILayout.Label("Player Mods", header);
            godmode = GUILayout.Toggle(godmode, " Godmode");
            nofall = GUILayout.Toggle(nofall, " No Fall");
            disableCameraShake = GUILayout.Toggle(disableCameraShake, " No Shake");
            nosmoke = GUILayout.Toggle(nosmoke, " No Smoke");
            multispeed = GUILayout.Toggle(multispeed, " Multi Speed");
            GUILayout.Label(" Speed Multiplier");
            speedmulti = GUILayout.HorizontalSlider(speedmulti, 1f, 10f);

            GUILayout.Space(8);
            GUILayout.Label("Vehicle Mods", header);
            vehiclegodmod = GUILayout.Toggle(vehiclegodmod, " Vehicle Godmode");

            GUILayout.Space(8);
            GUILayout.Label("Items", header);
            infammo = GUILayout.Toggle(infammo, " Infinite Ammo");
            instahit = GUILayout.Toggle(instahit, " Instant Hit");
            piercingbullet = GUILayout.Toggle(piercingbullet, " Piercing Bullet");
            norecoil = GUILayout.Toggle(norecoil, " No Recoil");
            nospread = GUILayout.Toggle(nospread, " No Spread");
            rapidfire = GUILayout.Toggle(rapidfire, " Rapid Fire");
            multidamage = GUILayout.Toggle(multidamage, " Multi Damage");
            GUILayout.Label(" Damage Multiplier");
            damagemulti = GUILayout.HorizontalSlider(damagemulti, 50f, 300f);
            GUILayout.EndVertical();
        }
        bool ColorToggle(bool toggle, Color color, string label, System.Action<Color> setColor)
        {
            GUILayout.BeginHorizontal();
            toggle = GUILayout.Toggle(toggle, label, GUILayout.Width(180));

            GUIStyle colorStyle = new GUIStyle(GUI.skin.button);
            colorStyle.normal.background = CreateColorTex(color);
            colorStyle.fixedWidth = 18;
            colorStyle.fixedHeight = 18;
            colorStyle.margin = new RectOffset(6, 6, 2, 2);

            if (GUILayout.Button("", colorStyle))
            {
                showPalette = true;
                float px = Mathf.Clamp(menuRect.x + menuRect.width - 220, 10, Screen.width - 210);
                float py = Mathf.Clamp(menuRect.y + 80, 10, Screen.height - 150);
                paletteRect = new Rect(px, py, 200, 120);
                onColorSelected = setColor;
            }

            GUILayout.EndHorizontal();
            return toggle;
        }

        void DrawPalette(int id)
        {
            GUILayout.Label("Preset Colors:");

            int cols = 5;
            int index = 0;

            GUILayout.BeginVertical();
            for (int r = 0; r < Mathf.CeilToInt((float)presetColors.Length / cols); r++)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < cols; c++)
                {
                    if (index < presetColors.Length)
                    {
                        Color col = presetColors[index];
                        GUIStyle style = new GUIStyle(GUI.skin.button);
                        style.normal.background = CreateColorTex(col);

                        if (GUILayout.Button("", style, GUILayout.Width(30), GUILayout.Height(30)))
                        {
                            onColorSelected?.Invoke(col);
                            showPalette = false;
                        }
                        index++;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("Close"))
                showPalette = false;

            GUI.DragWindow();
        }
        Texture2D CreateColorTex(Color c)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        Texture2D CreateSolidTex(Color c)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.DontSave;
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        public void DrawBoxESP(Vector3 foot, Vector3 head, Color color)
        {
            float h = head.y - foot.y;
            float w = h / 2f;
            Render.DrawBox(foot.x - (w / 2), Screen.height - foot.y - h, w, h, color, 1f);
        }

        public void DrawTraceline(Vector3 footpos, Vector3 headpos, Color color)
        {
            float height = headpos.y - footpos.y;
            float widthOffset = 2f;
            float width = height / widthOffset;

            Render.DrawLine(new Vector2((float)(Screen.width / 2), (float)(Screen.height - 2)), new Vector2(footpos.x, (float)Screen.height - footpos.y), color, 1f);
        }
        void DrawCornerBox(float x, float y, float w, float h, Color color)
        {
            float lineW = w / 5f;
            float lineH = h / 5f;
            float thickness = 1.5f;

            Render.DrawLine(new Vector2(x, y), new Vector2(x + lineW, y), color, thickness);
            Render.DrawLine(new Vector2(x, y), new Vector2(x, y + lineH), color, thickness);

            Render.DrawLine(new Vector2(x + w - lineW, y), new Vector2(x + w, y), color, thickness);
            Render.DrawLine(new Vector2(x + w, y), new Vector2(x + w, y + lineH), color, thickness);

            Render.DrawLine(new Vector2(x, y + h - lineH), new Vector2(x, y + h), color, thickness);
            Render.DrawLine(new Vector2(x, y + h), new Vector2(x + lineW, y + h), color, thickness);

            Render.DrawLine(new Vector2(x + w - lineW, y + h), new Vector2(x + w, y + h), color, thickness);
            Render.DrawLine(new Vector2(x + w, y + h - lineH), new Vector2(x + w, y + h), color, thickness);
        }
        private void DrawVehicleBoxESP(Vector3 foot, Vector3 head, Color color)
        {
            float h = (head.y - foot.y) * 0.8f;
            float w = h;
            float x = foot.x - (w / 2);
            float y = Screen.height - foot.y - h;
            Render.DrawBox(x, y, w, h, color, 1.5f);
            lastVehicleBoxX = x;
            lastVehicleBoxY = y;
            lastVehicleBoxW = w;
            lastVehicleBoxH = h;
        }
        float GetBoxESPWidth(Vector3 foot, Vector3 head)
        {
            return (head.y - foot.y) / 2f;
        }
    }
}
