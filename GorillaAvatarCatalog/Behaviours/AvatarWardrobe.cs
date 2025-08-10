using GorillaAvatarCatalog.Models;
using GorillaAvatarCatalog.Tools;
using GorillaNetworking;
using Photon.Pun;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Avatar = GorillaAvatarCatalog.Models.Avatar;
using CosmeticSlots = GorillaNetworking.CosmeticsController.CosmeticSlots;

namespace GorillaAvatarCatalog.Behaviours
{
    [RequireComponent(typeof(CosmeticWardrobe))]
    public class AvatarWardrobe : MonoBehaviour
    {
        public EAvatarWardrobeState currentState;

        private int loadedAvatarPage = 0, savedAvatarPage = 0;

        private CosmeticWardrobe cosmeticWardrobe;

        private ClientButton outfitSaveButton, outfitLoadButton;

        public void Awake()
        {
            cosmeticWardrobe = GetComponent<CosmeticWardrobe>();

            GameObject templateButton = cosmeticWardrobe.cosmeticCategoryButtons.First()?.button?.gameObject;

            InstantiateButton(templateButton, out GameObject outfitSaveButtonObject, out List<TMP_Text> outfitSaveText);
            outfitSaveButtonObject.transform.localPosition = new Vector3(0.255f, -0.195f, -0.045f);
            outfitSaveButtonObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.08f);
            outfitSaveText.ForEach(text =>
            {
                text.text = "SAVE\nAVATAR";
                text.lineSpacing = 50;
            });

            CosmeticCategoryButton component = outfitSaveButtonObject.GetComponent<CosmeticCategoryButton>();
            outfitSaveButton = outfitSaveButtonObject.AddComponent<ClientButton>();
            outfitSaveButton.ButtonRenderer = outfitSaveButtonObject.GetComponent<MeshRenderer>();
            outfitSaveButton.PressedMaterial = component.pressedMaterial;
            outfitSaveButton.UnpressedMaterial = component.unpressedMaterial;
            Destroy(component);
            outfitSaveButton.ButtonActivated += (isLeftHand) => SwitchState(EAvatarWardrobeState.SaveAvatar);

            InstantiateButton(templateButton, out GameObject outfitLoadButtonObject, out List<TMP_Text> outfitLoadText);
            outfitLoadButtonObject.transform.localPosition = new Vector3(0.255f, -0.195f + 0.06f, -0.045f);
            outfitLoadButtonObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.08f);
            outfitLoadText.ForEach(text =>
            {
                text.text = "LOAD\nAVATAR";
                text.lineSpacing = 50;
            });

            component = outfitLoadButtonObject.GetComponent<CosmeticCategoryButton>();
            outfitLoadButton = outfitLoadButtonObject.AddComponent<ClientButton>();
            outfitLoadButton.ButtonRenderer = outfitLoadButtonObject.GetComponent<MeshRenderer>();
            outfitLoadButton.PressedMaterial = component.pressedMaterial;
            outfitLoadButton.UnpressedMaterial = component.unpressedMaterial;
            Destroy(component);
            outfitLoadButton.ButtonActivated += (isLeftHand) => SwitchState(EAvatarWardrobeState.LoadAvatar);
        }

        public void SwitchState(EAvatarWardrobeState newState)
        {
            if (currentState == newState)
                newState = EAvatarWardrobeState.None;

            currentState = newState;

            outfitLoadButton.Activated = currentState == EAvatarWardrobeState.LoadAvatar;
            outfitLoadButton.UpdateColour();

            outfitSaveButton.Activated = currentState == EAvatarWardrobeState.SaveAvatar;
            outfitSaveButton.UpdateColour();

            switch (currentState)
            {
                case EAvatarWardrobeState.LoadAvatar:
                case EAvatarWardrobeState.SaveAvatar:
                    DisplayAvatars();
                    break;
                default:
                    cosmeticWardrobe.cosmeticCollectionDisplays.ForEach(selection => selection.displayHead._ClearCurrent());
                    cosmeticWardrobe.UpdateCategoryButtons();
                    cosmeticWardrobe.UpdateCosmeticDisplays();
                    break;
            }
        }

        public void DisplayAvatars()
        {
            cosmeticWardrobe.nextSelection.enabled = true;
            cosmeticWardrobe.nextSelection.UpdateColor();

            cosmeticWardrobe.prevSelection.enabled = true;
            cosmeticWardrobe.prevSelection.UpdateColor();

            for (int i = 0; i < cosmeticWardrobe.cosmeticCollectionDisplays.Length; i++)
            {
                var selection = cosmeticWardrobe.cosmeticCollectionDisplays[i];

                int startPosition = currentState switch
                {
                    EAvatarWardrobeState.LoadAvatar => loadedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                    EAvatarWardrobeState.SaveAvatar => savedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                    _ => 0
                };

                Avatar displayedAvatar = Preferences.Instance.GetValue<Avatar>($"Avatar {i + 1 + startPosition}", null);

                if (displayedAvatar == null || !displayedAvatar.IsValid)
                {
                    if (displayedAvatar != null)
                    {
                        Logging.Info($"Removed invalid avatar #{i + 1 + startPosition}");
                        Preferences.Instance.DeleteKey($"Avatar {i + 1 + startPosition}");
                    }

                    selection.displayHead._ClearCurrent();

                    selection.selectButton.enabled = currentState == EAvatarWardrobeState.SaveAvatar;
                    selection.selectButton.isOn = false;
                    selection.selectButton.UpdateColor();

                    continue;
                }

                CosmeticsController cosmeticsController = CosmeticsController.instance;
                var cosmeticSet = new CosmeticsController.CosmeticSet();
                for (int j = 0; j < cosmeticSet.items.Length; j++)
                {
                    CosmeticSlots slot = (CosmeticSlots)j;
                    cosmeticSet.items[j] = displayedAvatar.Cosmetics.TryGetValue(slot, out string itemName) ? cosmeticsController.GetItemFromDict(cosmeticsController.GetItemNameFromDisplayName(itemName)) : cosmeticsController.nullItem;
                }
                selection.displayHead.SetCosmeticActiveArray([.. cosmeticSet.items.Select(item => item.displayName)], cosmeticSet.ToOnRightSideArray());

                selection.selectButton.enabled = true;
                selection.selectButton.isOn = currentState == EAvatarWardrobeState.SaveAvatar;
                selection.selectButton.UpdateColor();
            }
        }

        public void HandleCosmeticsUpdated()
        {
            DisplayAvatars();
        }

        public void HandlePressedNextSelection()
        {
            switch (currentState)
            {
                case EAvatarWardrobeState.LoadAvatar:
                    loadedAvatarPage++;
                    DisplayAvatars();
                    break;
                case EAvatarWardrobeState.SaveAvatar:
                    savedAvatarPage++;
                    DisplayAvatars();
                    break;
            }
        }

        public void HandlePressedPrevSelection()
        {
            switch (currentState)
            {
                case EAvatarWardrobeState.LoadAvatar:
                    loadedAvatarPage--;
                    DisplayAvatars();
                    break;
                case EAvatarWardrobeState.SaveAvatar:
                    savedAvatarPage--;
                    DisplayAvatars();
                    break;
            }
        }

        public void HandlePressedSelectCosmeticButton(GorillaPressableButton button)
        {
            for (int i = 0; i < cosmeticWardrobe.cosmeticCollectionDisplays.Length; i++)
            {
                var selection = cosmeticWardrobe.cosmeticCollectionDisplays[i];
                if (selection.selectButton == button)
                {
                    Avatar avatar;
                    CosmeticsController cosmeticsController;
                    int startPosition;

                    switch (currentState)
                    {
                        case EAvatarWardrobeState.LoadAvatar:
                            bool inProximity = !NetworkSystem.Instance.InRoom || GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.GetMyUserID()) || CosmeticWardrobeProximityDetector.IsUserNearWardrobe(NetworkSystem.Instance.GetMyUserID());
                            if (inProximity)
                            {
                                startPosition = currentState switch
                                {
                                    EAvatarWardrobeState.LoadAvatar => loadedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                                    EAvatarWardrobeState.SaveAvatar => savedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                                    _ => 0
                                };

                                avatar = Preferences.Instance.GetValue<Avatar>($"Avatar {i + 1 + startPosition}", null);
                                if (avatar != null)
                                {
                                    Logging.Info($"Loading avatar #{i + 1}");

                                    // colour

                                    float redValue = Mathf.Clamp01(avatar.PlayerColour.Red / 9f);
                                    PlayerPrefs.SetFloat("redValue", redValue);

                                    float greenValue = Mathf.Clamp01(avatar.PlayerColour.Green / 9f);
                                    PlayerPrefs.SetFloat("greenValue", greenValue);

                                    float blueValue = Mathf.Clamp01(avatar.PlayerColour.Blue / 9f);
                                    PlayerPrefs.SetFloat("blueValue", blueValue);

                                    GorillaTagger.Instance.UpdateColor(redValue, greenValue, blueValue);
                                    GorillaComputer.instance.UpdateColor(redValue, greenValue, blueValue);

                                    PlayerPrefs.Save();

                                    if (NetworkSystem.Instance.InRoom)
                                    {
                                        GorillaTagger.Instance.myVRRig.SendRPC(nameof(VRRigSerializer.RPC_InitializeNoobMaterial), RpcTarget.All, redValue, greenValue, blueValue);
                                    }

                                    // name
                                    GorillaComputer.instance.currentName = avatar.PlayerName;
                                    GorillaComputer.instance.OnPlayerNameChecked(new ExecuteFunctionResult
                                    {
                                        FunctionResult = new JsonObject
                                        {
                                            ["result"] = (int)GorillaComputer.NameCheckResult.Success
                                        }
                                    });

                                    // cosmetics
                                    cosmeticsController = CosmeticsController.instance;

                                    for (int j = 0; j < cosmeticsController.currentWornSet.items.Length; j++)
                                    {
                                        CosmeticSlots slot = (CosmeticSlots)j;
                                        cosmeticsController.currentWornSet.items[j] = avatar.Cosmetics.TryGetValue(slot, out string itemName) ? cosmeticsController.GetItemFromDict(cosmeticsController.GetItemNameFromDisplayName(itemName)) : cosmeticsController.nullItem;
                                    }

                                    cosmeticsController.SaveCurrentItemPreferences();

                                    cosmeticsController.UpdateShoppingCart();
                                    cosmeticsController.UpdateWornCosmetics(true);

                                    cosmeticsController.OnCosmeticsUpdated?.Invoke();
                                    // cosmeticWardrobe.HandleCosmeticsUpdated();

                                    Logging.Info("Avatar loaded");
                                    DisplayAvatars();
                                }
                            }
                            break;
                        case EAvatarWardrobeState.SaveAvatar:
                            startPosition = currentState switch
                            {
                                EAvatarWardrobeState.LoadAvatar => loadedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                                EAvatarWardrobeState.SaveAvatar => savedAvatarPage * cosmeticWardrobe.cosmeticCollectionDisplays.Length,
                                _ => 0
                            };

                            Logging.Info($"Saving avatar #{i + 1 + startPosition}");

                            avatar = new()
                            {
                                PlayerName = GorillaComputer.instance.savedName,
                                PlayerColour = GorillaTagger.Instance.offlineVRRig.playerColor,
                                Cosmetics = []
                            };

                            cosmeticsController = CosmeticsController.instance;

                            for (int j = 0; j < cosmeticsController.currentWornSet.items.Length; j++)
                            {
                                var item = cosmeticsController.currentWornSet.items[j];
                                Logging.Info(item.itemName);
                                if (item.itemName != cosmeticsController.nullItem.itemName && cosmeticsController.unlockedCosmetics.Contains(item))
                                {
                                    CosmeticSlots slot = (CosmeticSlots)j;
                                    string displayName = cosmeticsController.GetItemDisplayName(item);
                                    Logging.Info($"Saving {displayName} to slot {slot}");
                                    avatar.Cosmetics.TryAdd(slot, displayName);
                                }
                            }

                            Preferences.Instance.SetKey($"Avatar {i + 1 + startPosition}", avatar);
                            CosmeticsController.instance.OnCosmeticsUpdated?.Invoke();

                            Logging.Info("Avatar saved");
                            break;
                    }
                    break;
                }
            }
        }

        private static void InstantiateButton(GameObject templateButton, out GameObject newButton, out List<TMP_Text> newTextList)
        {
            if (templateButton == null)
                throw new ArgumentNullException(nameof(templateButton), $"{nameof(templateButton)} is null");

            newButton = Instantiate(templateButton);
            newTextList = [];

            newButton.transform.SetParent(templateButton.transform.parent);
            newButton.transform.localPosition = templateButton.transform.localPosition;
            newButton.transform.localEulerAngles = templateButton.transform.localEulerAngles;

            if (newButton.TryGetComponent(out CosmeticCategoryButton component))
            {
                component.enabled = true;
                component.isOn = false;
                component.UpdateColor();
                Destroy(component);

                CosmeticCategoryButton component2 = templateButton.GetComponent<CosmeticCategoryButton>();

                if (component2.myTmpText is TMP_Text tmpText)
                {
                    Transform newTmpText = Instantiate(tmpText.gameObject).transform;

                    newTmpText.SetParent(newButton.transform);
                    newTmpText.position = tmpText.transform.position;
                    newTmpText.eulerAngles = tmpText.transform.eulerAngles;

                    if (newTmpText.TryGetComponent(out ReparentOnAwakeWithRenderer reparentShit))
                        Destroy(reparentShit);

                    newTextList.Add(newTmpText.GetComponent<TMP_Text>());
                }

                if (component2.myTmpText2 is TMP_Text tmpText2)
                {
                    Transform newTmpText = Instantiate(tmpText2.gameObject).transform;

                    newTmpText.SetParent(newButton.transform);
                    newTmpText.position = tmpText2.transform.position;
                    newTmpText.eulerAngles = tmpText2.transform.eulerAngles;

                    if (newTmpText.TryGetComponent(out ReparentOnAwakeWithRenderer reparentShit))
                        Destroy(reparentShit);

                    newTextList.Add(newTmpText.GetComponent<TMP_Text>());
                }
            }
        }
    }
}
