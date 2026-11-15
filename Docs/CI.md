# CI (GitHub Actions)

Deux workflows, pensés pour rester dans le quota gratuit de GitHub (2 000 minutes Linux par mois sur un repo privé).

| Workflow | Fichier | Quand | Coût |
|---|---|---|---|
| **Checks** | `.github/workflows/checks.yml` | Chaque push, chaque PR vers `main` | ~10 s, sans Unity |
| **Unity tests** | `.github/workflows/unity-tests.yml` | PR ou push sur `main` qui touche `Assets/`, `Packages/` ou `ProjectSettings/`, ou à la main | ~5–15 min |

## Checks (niveau 1)

- Chaque fichier de `Assets/` a son `.meta`, et aucun `.meta` n'est orphelin.
- Aucun fichier de plus de 10 Mo commité hors Git LFS.
- Formatage C# dans `Assets/_Project` (`dotnet format whitespace`).

En local :

```bash
bash .github/scripts/check-meta.sh Assets
bash .github/scripts/check-large-files.sh 10
dotnet format whitespace --folder Assets/_Project
```

## Unity tests (niveau 2)

Lance les tests EditMode avec [GameCI](https://game-ci.com/docs/github/test-runner). Les tests PlayMode se lancent à la main depuis l'onglet Actions (*Run workflow* → `playmode` ou `all`).

Les tests sont dans `Assets/_Project/Tests/EditMode`. `DataValidationTests` vérifie chaque asset de `Assets/_Project/Data` : pas de script manquant, pas de référence vers un asset supprimé. Chaque nouvelle règle sur les données (IDs uniques, ingrédients de recette existants, quêtes atteignables…) s'ajoute ici.

### Configuration unique : secrets de licence Unity

Sans ces secrets, le workflow n'échoue pas : il affiche un avertissement et saute les tests.

Dans GitHub : **Settings → Secrets and variables → Actions → New repository secret**

| Secret | Valeur |
|---|---|
| `UNITY_EMAIL` | Email du compte Unity |
| `UNITY_PASSWORD` | Mot de passe du compte Unity |
| `UNITY_LICENSE` | Contenu complet du fichier de licence (la licence Personal fonctionne) |

Le fichier de licence se trouve sur le PC une fois la licence activée par Unity Hub :
`C:\ProgramData\Unity\Unity_lic.ulf`. L'ouvrir dans un éditeur de texte et coller tout son contenu dans le secret.
Guide complet : https://game-ci.com/docs/github/activation

## Rester dans le quota gratuit

Déjà en place :

- Runners Linux uniquement (les minutes Windows comptent double, macOS dix fois).
- Filtres `paths` : modifier `Docs/` ne lance jamais Unity.
- `concurrency` + `cancel-in-progress` : un nouveau push annule le run précédent.
- `timeout-minutes` sur chaque job.
- Cache de `Library/` (évite un réimport complet) et cache LFS (économise la bande passante LFS).
- Artifacts conservés 3 jours.

Suivre la consommation : **github.com → Settings → Billing and plans → Usage**.
Sans moyen de paiement (ou avec une limite de dépense à 0 $), dépasser le quota met simplement les workflows en pause jusqu'au mois suivant. Rien n'est facturé.

## Plus tard

- **Builds** (WebGL → itch.io ou GitHub Pages, Windows) : `game-ci/unity-builder`, uniquement sur un tag `v*` ou à la main.
- **Runner auto-hébergé** sur le PC : aucune minute GitHub consommée, mais le PC doit être allumé.
