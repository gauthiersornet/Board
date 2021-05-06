using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public class BibliothèqueModel
    {
        private Dictionary<string, Model2_5D> DModel = new Dictionary<string, Model2_5D>();
        private Dictionary<string, Model2_5D> DModelInco = new Dictionary<string, Model2_5D>();//Figurines inconnues

        public Model2_5D ChargerModel2_5D(string path, XmlNode paq, BibliothèqueImage bibliothèqueImage)
        {
            Model2_5D mod = Model2_5D.ChargerModel2_5D(path, paq, bibliothèqueImage);
            if (DModel.ContainsKey(mod.Tag)) return DModel[mod.Tag];
            else
            {
                DModel.Add(mod.Tag, mod);
                return mod;
            }
        }

        public Model2_5D RécupérerOuCréerModel(string sig)
        {
            if (DModel.ContainsKey(sig)) return DModel[sig];
            else if (DModelInco.ContainsKey(sig)) return DModelInco[sig];
            else
            {
                Model2_5D mod = Model2_5D.CréerModelVide(sig);
                DModelInco[sig] = mod;
                return mod;
            }
        }

        public bool NouvelleVersion(string sig, Model2_5D mod)
        {
            Model2_5D orMod;
            if (DModelInco.ContainsKey(sig))
            {
                orMod = DModelInco[sig];
                DModelInco.Remove(sig);
            }
            else if (DModel.ContainsKey(sig)) orMod = DModel[sig];
            else
            {
                DModel[sig] = mod;
                return true;
            }

            orMod.Copier(mod);
            DModel[sig] = orMod;
            return true;
        }

        public string PremierModelInconnue { get => DModelInco.Keys.FirstOrDefault(); }
        public List<string> ModelInconnues { get => DModelInco.Keys.ToList(); }

        public void Netoyer()
        {
            DModel.Clear();
            DModelInco.Clear();
        }
    }
}
