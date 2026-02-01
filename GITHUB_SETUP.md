# GitHub Setup Instructions

## Prerequisites
- GitHub account (create one at https://github.com if needed)
- Git installed locally (already configured with your credentials)
- This local repository ready to push

## Step-by-Step Setup

### 1. Create Repository on GitHub

1. Go to https://github.com/new
2. Fill in the details:
   - **Repository name**: `dbc-localizer` (or your preferred name)
   - **Description**: "Automated German text localization for World of Warcraft 3.3.5 MPQ files"
   - **Visibility**: Choose `Public` or `Private`
   - **Add .gitignore**: No (we already have one)
   - **Add a license**: Choose a license (MIT recommended for open-source)

3. Click "Create repository"

### 2. Connect Local Repository to GitHub

After creating the GitHub repository, you'll see commands like these. Run them in PowerShell:

```powershell
cd D:\Spiele\WOW\Editor\DBC-Localizer

# Add remote origin
git remote add origin https://github.com/YOUR_USERNAME/dbc-localizer.git

# Rename branch to main (optional, GitHub uses 'main' by default)
git branch -M main

# Push to GitHub
git push -u origin main
```

**Replace `YOUR_USERNAME` with your actual GitHub username!**

### 3. Verify Upload

1. Go to your repository URL: `https://github.com/YOUR_USERNAME/dbc-localizer`
2. You should see all your files uploaded
3. The README.md will be displayed as the main page

## Optional: Add More Information

### GitHub Topics
On your repo page, click "Add topics" and add:
- `wow`
- `world-of-warcraft`
- `dbc`
- `mpq`
- `localizer`
- `wotlk`
- `csharp`
- `dotnet`

### GitHub Description
Under "About" (right side):
- Short description: "DBC Localizer for WoW 3.3.5"
- Website: Leave blank or add your website
- Topics: Add relevant tags

## Future Workflow

### After initial setup, for future changes:

```powershell
cd D:\Spiele\WOW\Editor\DBC-Localizer

# Make changes to files...

# Stage and commit
git add .
git commit -m "Your commit message"

# Push to GitHub
git push
```

## Creating Releases

When you have a stable version:

```powershell
# Create a tag
git tag -a v1.0.0 -m "First stable release"

# Push the tag
git push origin v1.0.0
```

Then go to your GitHub repo → "Releases" and create a release from the tag with compiled binaries.

## Collaboration

To let others contribute:
1. On GitHub, go to Settings → Collaborators
2. Add collaborators or let them fork and create pull requests

## Troubleshooting

### "remote origin already exists"
```powershell
git remote remove origin
git remote add origin https://github.com/YOUR_USERNAME/dbc-localizer.git
```

### "Authentication failed"
Use a Personal Access Token instead of password:
1. Generate token: https://github.com/settings/tokens
2. Use token as password in Git

### "Permission denied"
Make sure you're using the right authentication method (HTTPS with token or SSH key)

---

Need help? Create an issue on your GitHub repository!
