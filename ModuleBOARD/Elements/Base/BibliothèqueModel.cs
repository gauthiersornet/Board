using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public class BibliothèqueModel
    {
        private Dictionary<string, Model2_5D> DModel = new Dictionary<string, Model2_5D>();
        //private Dictionary<string, Model2_5D> DModelInco = new Dictionary<string, Model2_5D>();//Figurines inconnues
        //private HashSet<string> HSetModelInco = new HashSet<string>();
        private SortedSet<string> LstModelInco = new SortedSet<string>();

        static public Model2_5D ChargerModel(Stream stream, BibliothèqueImage bibImg)
        {
            Model2_5D mdl = new Model2_5D(stream, bibImg);
            if (mdl!= null  && mdl.Tag.Length >= 28)
                return mdl;
                //return NouvelleVersion(mdl);
            else return null;
        }

        public bool RécupérerModel(string sig, Stream stream)
        {
            lock (DModel)
            {
                Model2_5D mdl;
                if (DModel.TryGetValue(sig, out mdl) && mdl.EstConnue)
                {
                    mdl.Sérialiser(stream);
                    return true;
                }
            }
            return false;
        }

        public Model2_5D ChargerModel2_5D(string path, XmlNode paq, BibliothèqueImage bibliothèqueImage)
        {
            Model2_5D mod = Model2_5D.ChargerModel2_5D(path, paq, bibliothèqueImage);
            lock (DModel)
            {
                if (DModel.ContainsKey(mod.Tag)) return DModel[mod.Tag];
                else
                {
                    DModel.Add(mod.Tag, mod);
                    return mod;
                }
            }
        }

        public Model2_5D RécupérerOuCréerModel(string sig)
        {
            Model2_5D mod;
            lock (DModel)
            {
                if (DModel.TryGetValue(sig, out mod)) return mod;
            }

            mod = Model2_5D.CréerModelVide(sig);
            lock (DModel)
            {
                DModel[sig] = mod;
            }
            lock (LstModelInco) { LstModelInco.Add(sig); }
            return mod;
        }

        public void MettreAJour(Image img)
        {
            lock (DModel)
            {
                foreach (KeyValuePair<string, Model2_5D> kv in DModel)
                    kv.Value.MettreAJour(img);
            }
        }

        public bool NouvelleVersion(Model2_5D mod)
        {
            if (mod == null) return false;

            lock (LstModelInco) { LstModelInco.Remove(mod.Tag); }
            lock (DModel)
            {
                Model2_5D orMod;
                if (DModel.TryGetValue(mod.Tag, out orMod))
                    lock (orMod) { orMod.Copier(mod); }
                else DModel[mod.Tag] = mod;
            }
            return true;
        }

        //public string PremierModelInconnue { get { lock (DModelInco) { return DModelInco.Keys.FirstOrDefault(); } } }
        public List<string> ModelInconnues
        {
            get
            {
                List<string> res;
                lock (LstModelInco)
                {
                    res = LstModelInco.ToList();
                    LstModelInco.Clear();
                }
                return res;
            }
        }

        public void Netoyer()
        {
            lock (DModel) { DModel.Clear(); }
            lock (LstModelInco) { LstModelInco.Clear(); }
        }
    }
}
