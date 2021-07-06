using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class carpsScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> gridbuttons;
    public List<KMSelectable> selectors;
    public List<KMSelectable> submitbuttons;
    public Renderer[] grends;
    public Renderer[] buttonrings;
    public Material[] mats;

    private int[][,] grid = new int[3][,] { new int[8, 6], new int[8, 6], new int[8, 6] };
    private int selection;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        for (int i = 0; i < 48; i++)
        {
            int r = Random.Range(0, 4);
            for(int j = 0; j < 3; j++)
                grid[j][i / 6, i % 6] = r;
            grends[i].material = mats[r];
        }
        for(int i = 0; i < 48; i++)
        {
            int q = 0;
            int[] adj = new int[8];
            for(int j = -1; j < 2; j++)
                for(int k = -1; k < 2; k++)
                {
                    if (j == 0 && k == 0)
                        continue;
                    adj[q] = grid[0][((i / 6) + 8 + j) % 8, ((i % 6) + 6 + k) % 6];
                    q++;
                }
            int[] adjnum = Enumerable.Range(1, 3).Select(x => adj.Count(y => y == x)).ToArray();
            if(grid[0][i / 6, i % 6] == 0)
            {
                if (adjnum.Distinct().Count() < 2 || adjnum.All(x => x == 0))
                    continue;
                else if (adjnum.Distinct().Count() < 3)
                {
                    if (adjnum[0] == adjnum[1])
                        grid[1][i / 6, i % 6] = 2;
                    else if (adjnum[1] == adjnum[2])
                        grid[1][i / 6, i % 6] = 3;
                    else
                        grid[1][i / 6, i % 6] = 1;
                }
                else
                    grid[1][i / 6, i % 6] = adjnum.ToList().IndexOf(adjnum.Max()) + 1;
            }
            else
            {
                if(adjnum[grid[0][i / 6, i % 6] % 3] >= adjnum[(grid[0][i / 6, i % 6] + 1) % 3])
                {
                    grid[1][i / 6, i % 6] %= 3;
                    grid[1][i / 6, i % 6]++;
                }
            }
        }
        Debug.LogFormat("[CA-RPS #{0}] The initial grid:\n[CA-RPS #{0}] {1}", moduleID, string.Join("\n[CA-RPS #" + moduleID + "] ", Enumerable.Range(0, 8).Select(x => string.Join("", Enumerable.Range(0, 6).Select(z => "XRPS"[grid[0][x, z]].ToString()).ToArray())).ToArray()));
        Debug.LogFormat("[CA-RPS #{0}] The target grid:\n[CA-RPS #{0}] {1}", moduleID, string.Join("\n[CA-RPS #" + moduleID + "] ", Enumerable.Range(0, 8).Select(x => string.Join("", Enumerable.Range(0, 6).Select(z => "XRPS"[grid[1][x, z]].ToString()).ToArray())).ToArray()));
        foreach (KMSelectable cell in gridbuttons)
        {
            int g = gridbuttons.IndexOf(cell);
            cell.OnInteract = delegate () { Cell(g); return false; };
        }
        foreach(KMSelectable selector in selectors)
        {
            int s = selectors.IndexOf(selector);
            selector.OnInteract = delegate () { ChangeSelection(s); return false; };
        }
        foreach(KMSelectable button in submitbuttons)
        {
            int b = submitbuttons.IndexOf(button);
            button.OnInteract = delegate () { Submit(b); return false; };
        }
    }

    private void Cell(int g)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform("Select" + selection, gridbuttons[g].transform);
            grid[2][g / 6, g % 6] = selection;
            grends[g].material = mats[selection];
        }
    }

    private void ChangeSelection(int s)
    {
        if (!moduleSolved)
        {
            selectors[s].AddInteractionPunch(0.25f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, selectors[s].transform);
            if(selection > 0)
                buttonrings[selection - 1].material = mats[0];
            if (selection == s + 1)
                selection = 0;
            else
            {
                selection = s + 1;
                buttonrings[s].material = mats[s + 1];
            }
        }
    }

    private void Submit(int b)
    {
        if (!moduleSolved)
        {
            submitbuttons[b].AddInteractionPunch(0.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitbuttons[b].transform);
            if (b < 2)
            {
                if (b == 0)
                    for (int i = 0; i < 48; i++)
                        grid[2][i / 6, i % 6] = 0;
                else
                    for (int i = 0; i < 48; i++)
                        grid[2][i / 6, i % 6] = grid[0][i / 6, i % 6];
                for (int i = 0; i < 48; i++)
                    grends[i].material = mats[grid[2][i / 6, i % 6]];
            }
            else
            {
                if(Enumerable.Range(0, 48).Select(x => grid[1][x / 6, x % 6] == grid[2][x / 6, x % 6]).All(x => x))
                {
                    moduleSolved = true;
                    module.HandlePass();
                    buttonrings[selection - 1].material = mats[0];
                }
                else
                {
                    module.HandleStrike();
                }
            }
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} <RPSX> <a-f><1-8> [Selects rock, paper, scissors or empty respectively, and changes the state of the specified cells to the selected state. Commands can be chained with spaces.] | !{0} clear | !{0} reset | !{0} submit";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.ToLowerInvariant() == "clear")
        {
            yield return null;
            submitbuttons[0].OnInteract();
            yield break;
        }
        if (command.ToLowerInvariant() == "reset")
        {
            yield return null;
            submitbuttons[1].OnInteract();
            yield break;
        }
        if (command.ToLowerInvariant() == "submit")
        {
            yield return null;
            submitbuttons[2].OnInteract();
            yield break;
        }
        string[] commands = command.ToUpperInvariant().Split(' ');
        for (int i = 0; i < commands.Length; i++)
        {
            if (commands[i].Length == 1 && "RSPX".Contains(commands[i]))
                continue;
            if (commands[i].Length == 2 && commands[i][0] - 'A' >= 0 && commands[i][0] - 'A' < 6 && commands[i][1] - '1' >= 0 && commands[i][1] - '1' < 8)
                continue;
            yield return "sendtochaterror!f Invalid command: " + commands[i];
            yield break;
        }
        for (int i = 0; i < commands.Length; i++)
        {
            yield return null;
            if (commands[i].Length == 1)
            {
                if (commands[i] == "X" && selection > 0)
                    selectors[selection - 1].OnInteract();
                else
                {
                    int r = "#RPS".IndexOf(commands[i]);
                    if (selection != r)
                        selectors[r - 1].OnInteract();
                }
            }
            else
                gridbuttons[((commands[i][1] - '1') * 6) + commands[i][0] - 'A'].OnInteract();
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if(selection > 0)
            selectors[selection - 1].OnInteract();
        for(int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 48; j++)
                if (grid[1][j / 6, j % 6] == i)
                {
                    yield return null;
                    gridbuttons[j].OnInteract();
                }
            if (i < 3)
            {
                yield return null;
                selectors[i].OnInteract();
            }
        }
        yield return null;
        submitbuttons[2].OnInteract();
    }
}
