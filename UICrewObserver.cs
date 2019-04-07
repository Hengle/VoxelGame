﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICrewObserver : MonoBehaviour
{
    [SerializeField] private Dropdown shuttlesDropdown, artifactsDropdown;
    [SerializeField] private InputField nameField;
    [SerializeField] private Text levelText, membersButtonText, experienceText, statsText, statusText;
    [SerializeField] private Image experienceBar, staminaBar;
    [SerializeField] private Button membersButton, shuttleButton, artifactButton;
    [SerializeField] private GameObject dismissButton, travelButton;
    [SerializeField] private RawImage icon;

    private bool subscribedToUpdate = false;
    private int lastDrawState = 0, lastShuttlesState = 0, lastArtifactsState = 0;
    private List<int> shuttlesListIDs, artifactsIDs;
    private Crew showingCrew;

    public void ShowCrew(Crew c)
    {
        if (c == null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            showingCrew = c;
            RedrawWindow();
        }
    }

    private void RedrawWindow()
    {
        nameField.text = showingCrew.name;
        statsText.text = Localization.GetCrewInfo(showingCrew);
        PrepareShuttlesDropdown();
        PrepareArtifactsDropdown();
        lastDrawState = Crew.actionsHash;
    }

    public void StatusUpdate()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (lastDrawState != Crew.actionsHash)
            {
                RedrawWindow();
            }
            else
            {
                //#redraw dynamic crew info
                levelText.text = showingCrew.level.ToString();
                levelText.color = Color.Lerp(Color.white, Color.cyan, (float)showingCrew.level / 255f);
                float e = showingCrew.experience, ne = showingCrew.nextExperienceLimit;
                experienceText.text = ((int)e).ToString() + " / " + ((int)ne).ToString();
                experienceBar.fillAmount = e / ne;
                staminaBar.fillAmount = showingCrew.stamina;

                int m_count = showingCrew.membersCount;
                membersButtonText.text = m_count.ToString() + '/' + Crew.MAX_MEMBER_COUNT.ToString();
                membersButton.enabled = (m_count != Crew.MAX_MEMBER_COUNT) & showingCrew.status == CrewStatus.AtHome;

                shuttleButton.enabled = Shuttle.shuttlesList.Count > 0;
                var ri = shuttleButton.transform.GetChild(0).GetComponent<RawImage>();
                if (showingCrew.shuttle != null)
                {
                    showingCrew.shuttle.DrawShuttleIcon(ri);
                }
                else
                {
                    ri.texture = UIController.current.iconsTexture;
                    ri.uvRect = UIController.GetTextureUV(Icons.TaskFrame);
                }

                artifactButton.enabled = Artifact.playersArtifactsList.Count > 0;
                ri = artifactButton.transform.GetChild(0).GetComponent<RawImage>();
                if (showingCrew.artifact != null)
                {
                    ri.texture = showingCrew.artifact.GetTexture();
                    ri.uvRect = new Rect(0f, 0f, 1f, 1f);
                }
                else
                {
                    ri.texture = UIController.current.iconsTexture;
                    ri.uvRect = UIController.GetTextureUV(Icons.TaskFrame);
                }

                statusText.text = Localization.GetCrewStatus(showingCrew.status);
                dismissButton.SetActive(showingCrew.status == CrewStatus.AtHome);

                travelButton.SetActive(showingCrew.shuttle != null & showingCrew.status == CrewStatus.AtHome);
                // stamina check?

                if (lastShuttlesState != Shuttle.actionsHash) PrepareShuttlesDropdown();
                if (lastArtifactsState != Artifact.actionsHash) PrepareArtifactsDropdown();
            }

        }
    }

    //buttons
    public void NameChanged()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            showingCrew.Rename(nameField.text);
        }
    }
    public void MembersButton()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (RecruitingCenter.SelectAny()) gameObject.SetActive(false);
        }
    }
    public void ShuttleButton()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (showingCrew.shuttle == null) RedrawWindow();
            else
            {
                UIController.current.Select(showingCrew.shuttle.hangar);
                gameObject.SetActive(false);
            }
        }
    }
    public void ArtifactButton()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (showingCrew.artifact != null)
            {
                showingCrew.artifact.ShowOnGUI();
                gameObject.SetActive(false);
            }
            else RedrawWindow();
        }
    }
    public void TravelButton() //  NOT COMPLETED
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (showingCrew.status == CrewStatus.AtHome)
            {
                //
            }
            else
            {
                RedrawWindow();
            }
        }
    }
    public void DismissButton() // сделать подтверждение
    {
        if (showingCrew != null) showingCrew.Dismiss();
    }

    public void SelectShuttle(int i)
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else {
            if (shuttlesListIDs[i] == -1)
            {
                showingCrew.SetShuttle(null);
                PrepareShuttlesDropdown();
            }
            else
            {
                var s = Shuttle.GetShuttle(shuttlesListIDs[i]);
                if (s != null)
                {
                    if (showingCrew.shuttle == null || showingCrew.shuttle != s)
                    {
                        showingCrew.SetShuttle(s);
                        PrepareShuttlesDropdown();
                    }
                }
                else PrepareShuttlesDropdown();
            }
        }
    }
    public void SelectArtifact(int i)
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            if (artifactsIDs[i] == -1)
            {
                showingCrew.DropArtifact();
                PrepareArtifactsDropdown();
            }
            else
            {
                var s = Artifact.GetArtifactByID(artifactsIDs[i]);
                if (showingCrew.artifact == null || showingCrew.artifact != s)
                {
                    showingCrew.SetArtifact(s);
                    PrepareArtifactsDropdown();
                }
            }
        }
    }
    //

    public void LocalizeTitles()
    {
        dismissButton.transform.GetChild(0).GetComponent<Text>().text = Localization.GetWord(LocalizedWord.Dismiss);
        travelButton.transform.GetChild(0).GetComponent<Text>().text = Localization.GetPhrase(LocalizedPhrase.GoOnATrip);
    }

    private void PrepareShuttlesDropdown()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            var opts = new List<Dropdown.OptionData>();
            var shuttles = Shuttle.shuttlesList;
            shuttlesListIDs = new List<int>();
            opts.Add(new Dropdown.OptionData(Localization.GetPhrase(LocalizedPhrase.NoShuttle)));
            shuttlesListIDs.Add(-1);
            if (showingCrew.shuttle == null)
            { // без проверок на собственный шаттл                
                if (shuttles.Count > 0 )
                {
                    foreach (var s in shuttles)
                    {
                        if (s.crew == null)
                        {
                            opts.Add(new Dropdown.OptionData(s.name));
                            shuttlesListIDs.Add(s.ID);
                        }
                    }
                }
            }
            else
            {
                opts.Insert(0, new Dropdown.OptionData(showingCrew.shuttle.name));
                shuttlesListIDs.Insert(0, showingCrew.shuttle.ID);
                if (shuttles.Count > 0)
                {
                    foreach (var s in shuttles)
                    {
                        if (s != showingCrew.shuttle & s.crew == null)
                        {
                            opts.Add(new Dropdown.OptionData(s.name));
                            shuttlesListIDs.Add(s.ID);
                        }
                    }
                }
            }
            shuttlesDropdown.value = 0;
            shuttlesDropdown.options = opts;
            lastShuttlesState = Shuttle.actionsHash;
        }
    }
    private void PrepareArtifactsDropdown()
    {
        if (showingCrew == null) gameObject.SetActive(false);
        else
        {
            var opts = new List<Dropdown.OptionData>();
            var artifacts = Artifact.playersArtifactsList;
            artifactsIDs = new List<int>();
            opts.Add(new Dropdown.OptionData(Localization.GetPhrase(LocalizedPhrase.NoArtifact)));
            artifactsIDs.Add(-1);
            if (showingCrew.artifact == null)
            { // без проверок на собственный артефакт             
                if (artifacts.Count > 0)
                {
                    foreach (var s in artifacts)
                    {
                        if (s.status == Artifact.ArtifactStatus.OnConservation)
                        {
                            opts.Add(new Dropdown.OptionData(s.name));
                            artifactsIDs.Add(s.ID);
                        }
                    }
                }
            }
            else
            {
                opts.Insert(0, new Dropdown.OptionData(showingCrew.artifact.name));
                shuttlesListIDs.Insert(0, showingCrew.artifact.ID);
                if (artifactsIDs.Count > 0)
                {
                    foreach (var s in artifacts)
                    {
                        if (s.status == Artifact.ArtifactStatus.OnConservation)
                        {
                            opts.Add(new Dropdown.OptionData(s.name));
                            artifactsIDs.Add(s.ID);
                        }
                    }
                }
            }
            artifactsDropdown.value = 0;
            artifactsDropdown.options = opts;
            lastArtifactsState = Artifact.actionsHash;
        }
    }

    private void OnEnable()
    {
        if (!subscribedToUpdate)
        {
            UIController.current.statusUpdateEvent += StatusUpdate;
            subscribedToUpdate = true;
        }
    }
    private void OnDisable()
    {
        if (subscribedToUpdate)
        {
            if (UIController.current != null)
            {
                UIController.current.statusUpdateEvent -= StatusUpdate;
            }
            subscribedToUpdate = false;
        }
    }
    private void OnDestroy()
    {
        if (!GameMaster.sceneClearing & subscribedToUpdate)
        {
            if (UIController.current != null)
            {
                UIController.current.statusUpdateEvent -= StatusUpdate;
            }
            subscribedToUpdate = false;
        }
    }
}