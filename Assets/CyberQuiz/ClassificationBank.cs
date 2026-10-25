using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CyberQuiz
{
    public static class ClassificationBank
    {
        public const int RoundSize = 10;
        public const string FileName = "nfc-tags.json";
        public static readonly string[] Labels = { "OPEN", "SECRET", "CONFIDENTIAL", "SENSITIVE" };
        public static readonly string[] Tags = { "2435786112", "2435817104", "2435882544", "2435920016" };

        [Serializable]
        class NfcTagFile
        {
            public string comment;
            public string OPEN, SECRET, CONFIDENTIAL, SENSITIVE;
        }

        [Serializable]
        class LegacyNfcTagFile
        {
            public string A, B, C, D;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadTags()
        {
            try
            {
                var external = ExternalPath();
                var streamed = Path.Combine(Application.streamingAssetsPath, FileName);
                if (!File.Exists(external))
                {
                    if (File.Exists(streamed)) File.Copy(streamed, external, false);
                    else File.WriteAllText(external, JsonUtility.ToJson(FromTags(), true));
                }
                var path = File.Exists(external) ? external : streamed;
                if (!File.Exists(path)) return;
                if (ReadTags(File.ReadAllText(path), out bool legacy))
                {
                    Debug.Log("NFC tags loaded from "+path);
                    if (legacy) File.WriteAllText(path, JsonUtility.ToJson(FromTags(), true));
                }
            }
            catch (Exception e) { Debug.LogWarning("NFC tag file could not be read: "+e.Message); }
        }

        static NfcTagFile FromTags() => new NfcTagFile
        {
            comment = "NFC IDs for the classification cubes. Each value is the tag for that classification. Edit this file, save, then restart the game. No rebuild needed.",
            OPEN = Tags[0], SECRET = Tags[1], CONFIDENTIAL = Tags[2], SENSITIVE = Tags[3]
        };

        static bool ReadTags(string json, out bool legacy)
        {
            legacy = false;
            var data = JsonUtility.FromJson<NfcTagFile>(json);
            if (data == null) return false;
            if (Any(data.OPEN, data.SECRET, data.CONFIDENTIAL, data.SENSITIVE))
            {
                Apply("OPEN", data.OPEN);
                Apply("SECRET", data.SECRET);
                Apply("CONFIDENTIAL", data.CONFIDENTIAL);
                Apply("SENSITIVE", data.SENSITIVE);
                return true;
            }
            var old = JsonUtility.FromJson<LegacyNfcTagFile>(json);
            if (old == null || !Any(old.A, old.B, old.C, old.D)) return false;
            Apply("OPEN", old.A);
            Apply("SECRET", old.B);
            Apply("CONFIDENTIAL", old.C);
            Apply("SENSITIVE", old.D);
            legacy = true;
            return true;
        }

        static bool Any(params string[] values)
        {
            foreach (var value in values) if (!string.IsNullOrWhiteSpace(value)) return true;
            return false;
        }

        static void Apply(string label, string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return;
            for (int i = 0; i < Labels.Length; i++) if (Labels[i] == label) { Tags[i] = value; return; }
        }

        public static string TagFor(string label)
        {
            for (int i = 0; i < Labels.Length; i++) if (Labels[i] == label) return Tags[i];
            return "";
        }

        public static string LabelForTag(string value)
        {
            value = (value ?? "").Trim();
            for (int i = 0; i < Tags.Length; i++) if (Tags[i] == value) return Labels[i];
            return null;
        }

        public static string Folder
        {
            get
            {
                var folder = Directory.GetParent(Application.dataPath);
                return folder != null ? folder.FullName : Application.dataPath;
            }
        }
        public static string ExternalPath() => Path.Combine(Folder, FileName);
        public static string ResultsCsvPath => Path.Combine(Folder, "quiz-results.csv");

        public static string[] ShuffleClassifications(System.Random rng = null)
        {
            if (rng == null) rng = new System.Random();
            var order = (string[])Labels.Clone();
            Shuffle(order, rng);
            return order;
        }

        [Serializable]
        public class Item
        {
            public string id, prompt, answer, explanation;
            public Item(string id, string prompt, string answer, string explanation)
            {
                this.id = id; this.prompt = prompt; this.answer = answer; this.explanation = explanation;
            }
        }

        public static int IndexOf(string answer)
        {
            for (int i = 0; i < Labels.Length; i++) if (Labels[i] == answer) return i;
            return 0;
        }

        public static Item[] DrawRound(System.Random rng = null)
        {
            if (rng == null) rng = new System.Random();
            var groups = new[] { Open, Confidential, Sensitive, Secret };
            var counts = new[] { 2, 2, 2, 2 };
            var extra = new[] { 0, 1, 2, 3 };
            for (int i = extra.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int swap = extra[i]; extra[i] = extra[j]; extra[j] = swap;
            }
            counts[extra[0]]++;
            counts[extra[1]]++;
            var picked = new List<Item>(RoundSize);
            for (int g = 0; g < groups.Length; g++)
            {
                var copy = (Item[])groups[g].Clone();
                Shuffle(copy, rng);
                for (int i = 0; i < counts[g]; i++) picked.Add(copy[i]);
            }
            Shuffle(picked, rng);
            return picked.ToArray();
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var swap = list[i]; list[i] = list[j]; list[j] = swap;
            }
        }

        public static readonly Item[] Open =
        {
            new Item("DC-P01","A press release approved by the communications team and published on the organisation's official website. Which data security classification applies?","OPEN","This exact version has been approved and released for everyone to read."),
            new Item("DC-P02","The customer service opening hours displayed on the organisation's official website. Which data security classification applies?","OPEN","These opening hours are deliberately published for customers and visitors."),
            new Item("DC-P03","An approved brochure explaining available services, handed out to visitors at the reception desk. Which data security classification applies?","OPEN","The brochure is approved for distribution to visitors and the wider public."),
            new Item("DC-P04","A cybersecurity awareness poster officially published on the organisation's social media account. Which data security classification applies?","OPEN","The organisation has intentionally shared this approved poster with everyone."),
            new Item("DC-P05","A job advertisement officially published on the organisation's careers website, with no applicant information attached. Which data security classification applies?","OPEN","The advertisement is for open distribution and contains no private applicant records."),
            new Item("DC-P06","An event programme approved for everyone to view on the organisation's official event page. Which data security classification applies?","OPEN","This programme is approved for open access, not just for employees."),
            new Item("DC-P07","The approved annual report summary available for anyone to download from the organisation's website. Which data security classification applies?","OPEN","The published summary is the version approved for open distribution."),
            new Item("DC-P08","The main office address and reception phone number listed on the organisation's official contact page. Which data security classification applies?","OPEN","These general contact details are intentionally published for anyone to use."),
            new Item("DC-P09","A blank application form approved for download from the organisation's website, with no personal details filled in. Which data security classification applies?","OPEN","The blank template is openly available; a completed form could need a higher classification."),
            new Item("DC-P10","An approved step-by-step guide for using an online service, published on the organisation's help page for all users. Which data security classification applies?","OPEN","The guide is approved for all service users and contains no non-public instructions.")
        };

        public static readonly Item[] Confidential =
        {
            new Item("DC-I01","A staff guide explaining how to book meeting rooms. Which data security classification applies?","CONFIDENTIAL","This is non-public workplace guidance for staff to carry out an everyday task."),
            new Item("DC-I02","An individual employee's payslip showing their name, salary, allowances and deductions. Which data security classification applies?","CONFIDENTIAL","A payslip is a private employment record for the employee and authorised payroll staff, not for general circulation."),
            new Item("DC-I03","A general staff training calendar listing session topics and times, without individual attendance or assessment records. Which data security classification applies?","CONFIDENTIAL","The calendar supports staff planning and is intended for use within the organisation."),
            new Item("DC-I04","A staff announcement about office carpet cleaning, with no security procedures or entry codes included. Which data security classification applies?","CONFIDENTIAL","The notice concerns everyday office operations and is intended for employees, not for public release."),
            new Item("DC-I05","An internal employee newsletter containing major organizational updates and social activities. Which data security classification applies?","CONFIDENTIAL","The newsletter is a staff communication; its internal circulation does not make it approved for public release."),
            new Item("DC-I06","An employee's completed performance review containing their manager's ratings and comments. Which data security classification applies?","CONFIDENTIAL","This is an individual employment record for the employee, their manager and authorised HR staff."),
            new Item("DC-I07","A basic staff guide showing how to select a shared printer and print double-sided. Which data security classification applies?","CONFIDENTIAL","The guide supports everyday work and is intended for employees using the organisation's equipment."),
            new Item("DC-I08","A job applicant's CV containing their personal phone number, home address and employment history. Which data security classification applies?","CONFIDENTIAL","The CV is an individual recruitment record for authorised hiring staff, not for wider circulation."),
            new Item("DC-I09","A signed office-supply contract containing negotiated prices and payment terms that have not been published. Which data security classification applies?","CONFIDENTIAL","These non-public terms are for authorised procurement and finance staff, not for general distribution."),
            new Item("DC-I10","An employee's emergency contact form containing family members' names, relationships and personal phone numbers. Which data security classification applies?","CONFIDENTIAL","The form contains private contact information for authorised HR or emergency use, not a general staff directory.")
        };

        public static readonly Item[] Sensitive =
        {
            new Item("DC-C01","An unannounced plan to merge two government departments, including staffing and service changes before affected teams have been briefed. Which data security classification applies?","SENSITIVE","Premature disclosure could undermine a major organisational change affecting staff and government services."),
            new Item("DC-C02","A leadership briefing comparing options for a major new government service policy, before those options are announced or consulted on. Which data security classification applies?","SENSITIVE","The briefing exposes strategic choices before the government is ready to discuss them publicly."),
            new Item("DC-C03","A negotiation brief for a major government technology contract, showing the maximum budget and fallback terms before negotiations. Which data security classification applies?","SENSITIVE","The brief reveals the government's negotiating position and could seriously weaken its ability to secure favourable terms."),
            new Item("DC-C04","An unpublished audit report describing serious failings in spending approvals across several departments and planned corrective actions. Which data security classification applies?","SENSITIVE","The report exposes significant control problems across departments before corrective action is complete."),
            new Item("DC-C05","A file for an ongoing investigation into suspected large-scale procurement fraud, containing evidence and planned witness interviews. Which data security classification applies?","SENSITIVE","Disclosure could alert those involved, affect witness accounts and undermine the active investigation."),
            new Item("DC-C06","A government-wide programme review showing major cost overruns and delayed services, with unannounced corrective decisions awaiting approval. Which data security classification applies?","SENSITIVE","The review reveals significant delivery problems and leadership decisions affecting a major government programme."),
            new Item("DC-C07","A non-public continuity plan showing which government services would be restored first and how limited staff and resources would be allocated. Which data security classification applies?","SENSITIVE","The plan exposes service-restoration priorities and operational dependencies beyond routine workplace information."),
            new Item("DC-C08","An unpublished assessment that a key technology supplier may stop supporting several government services, with plans to replace that supplier. Which data security classification applies?","SENSITIVE","The assessment exposes a significant service-continuity risk and the government's unannounced response."),
            new Item("DC-C09","A non-public funding paper ranking major cross-government projects for possible deferral or cancellation before funding decisions are announced. Which data security classification applies?","SENSITIVE","The paper reveals strategic funding priorities that could affect several government entities and major projects."),
            new Item("DC-C10","An internal migration plan for a shared government digital platform, detailing the rollout sequence, expected service interruptions and fallback arrangements. Which data security classification applies?","SENSITIVE","The plan exposes important operational changes and service dependencies before the planned transition.")
        };

        public static readonly Item[] Secret =
        {
            new Item("DC-R01","The current administrator password that can reset every employee's work account. Which data security classification applies?","SECRET","This live access secret could give someone control over every employee's work account."),
            new Item("DC-R02","Unused recovery codes that allow access to the organisation's main administrator account. Which data security classification applies?","SECRET","These recovery codes can grant access to the main administrator account and require the highest protection."),
            new Item("DC-R03","The secret key needed to unlock backups containing the organisation's employee and customer records. Which data security classification applies?","SECRET","This key could unlock backups and expose many employee and customer records."),
            new Item("DC-R04","The active override code that disables the alarm and unlocks the main server room. Which data security classification applies?","SECRET","The code bypasses physical protection around the organisation's critical systems."),
            new Item("DC-R05","A security report describing an unfixed vulnerability that could let attackers take over an essential government service. Which data security classification applies?","SECRET","Disclosure could let attackers compromise an essential government service before the vulnerability is fixed."),
            new Item("DC-R06","A database export containing identity documents and medical records of high-ranking individuals. Which data security classification applies?","SECRET","These combined identity and medical records could expose high-ranking individuals to targeted harm."),
            new Item("DC-R07","A document identifying a protected whistleblower and their safe address, where disclosure could put them in danger. Which data security classification applies?","SECRET","Disclosure could reveal the protected person's location and put them in direct danger."),
            new Item("DC-R08","The private digital key that lets its holder sign official documents as the organisation. Which data security classification applies?","SECRET","Misuse could allow someone to impersonate the organisation in official approvals."),
            new Item("DC-R09","An emergency recovery file containing current access details for all critical business systems. Which data security classification applies?","SECRET","The combined access details could enable compromise of multiple critical business systems."),
            new Item("DC-R10","An unreleased security plan showing the exact route, timings and protective arrangements for a high-ranking individual's visit, where disclosure could enable an attack. Which data security classification applies?","SECRET","The exact timing, route and protective arrangements could enable a targeted attack and directly endanger a person.")
        };
    }
}
