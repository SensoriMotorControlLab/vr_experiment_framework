using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UXF;

public class ExperimentGenerator : MonoBehaviour
{
    public void GenerateBlocks(Session session)
    {
        var keys = session.settings.Keys;
        
        if (keys.First() == "session_1")
        {
            switch(session.number) 
            {
                case 1:
                    keys = session.settings.GetDict("session_1").Keys;
                    session.settings = new Settings(session.settings.GetDict("session_1"));
                    break;
                
                case 2:
                    keys = session.settings.GetDict("session_2").Keys;
                    session.settings = new Settings(session.settings.GetDict("session_2"));
                    break;
            }
        }

        List<int> perBlockN = session.settings.GetIntList("per_block_n");
        string experimentMode = session.settings.GetString("experiment_mode");

        // create blocks, increment indexes of List<int> perBlockN
        for (int i = 0; i < perBlockN.Count; i++)
        {
            session.CreateBlock(perBlockN[i]);
            session.blocks[i].settings.SetValue("experiment_mode", experimentMode);

            foreach (string key in keys)
            {
                if (key != "per_block_n" && key.StartsWith("per_"))
                {
                    session.blocks[i].settings.SetValue(key, session.settings.GetObjectList(key)[i]);
                }
            }
        }

        GetComponent<ExperimentController>().Init(session);
    }
}
