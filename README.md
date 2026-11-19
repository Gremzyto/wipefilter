# WipeFilter (application Windows)

Source de l'application WipeFilter : filtre de couleurs, viseur superposé, codes de partage (RFF-, RFX-, RUST-).

Compilation (Windows, .NET Framework 4 déjà présent, aucune dépendance) :

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Le résultat est dans `out\WipeFilter.exe`. Test intégré : `WipeFilter.exe --selftest`.
Le fichier publié en Release est accompagné de son empreinte SHA-256, affichée sur https://wipefilter.fr/wipefilter/.

## Téléchargement

La dernière version est dans l'onglet **Releases**. Site : https://wipefilter.fr/wipefilter/
