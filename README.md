# 🧬 AR Biology Flashcards App

## 📖 Overview  
**AR Biology Flashcards** is an educational Unity app that uses Augmented Reality (AR) to display interactive 3D models of animal, fungi, plant, and bacterial cells. When a user scans an image target, the app shows related biology facts and allows users to take quizzes to reinforce learning.

---

## ✨ Features

- **AR Image Tracking**: Detects and tracks reference images like `plantcell.jpg`, `animalcell.jpg`, etc., using ARFoundation.
- **Fact Display System**: Displays toggleable facts related to the detected cell type.
- **Quiz Mode**: Offers a multiple-choice quiz based on the displayed cell type.
- **Score Tracking**: Saves high scores for each quiz session (locally stored).
- **Responsive UI**: Optimized for iOS devices with 828x1792 resolution (e.g., iPhone 14).

---

## 🛠 Requirements

- **Unity**: Version 2020.3 LTS or later (recommended).
- **TextMeshPro**: Pre-installed in Unity (check via *Window > Package Manager*).
- **AR Foundation**: Install via Package Manager.
- **ARKit XR Plugin**: For iOS deployment.
- **iOS Device**: App tested on iPhone 14.

---

## 🚀 Setup Instructions

### 1. Open the Project
- Clone or download the zipped Unity project folder.
- Open it using Unity Hub.

### 2. Import Required Packages
- TextMeshPro (if not auto-imported).
- AR Foundation + ARKit XR Plugin (via *Window > Package Manager*).

### 3. Scene Configuration
- Ensure a **Canvas** exists with **Render Mode: Screen Space - Overlay**.
- Add **Canvas Scaler**:  
  - Reference Resolution: `828 x 1792`  
  - Match: `0.5`

### 4. UI Setup
- Create a **TextMeshPro UI Text** and rename it `FactTextNew`.
- Set its anchor to **Top Center**, position Y to `-100`, width to `500`, height to `150`.
- Assign the font asset (e.g., `LiberationSans SDF`).

### 5. Script Assignments
- Attach `UIManager.cs` to a GameObject (e.g., `UIManager`).
- Assign all required fields in the Inspector:
  - `FactTextNew`, `Show Fact Button`, `New Session Button`, `Take Quiz Button`
  - Quiz panel elements like `questionText`, `answerButtons`, etc.

### 6. AR Image Tracking
- Attach the `ImageTracking.cs` script to a GameObject with `ARTrackedImageManager`.
- Add your reference images (e.g., `plantcell.jpg`) to the *Tracked Image Library*.

### 7. Build for iOS
- Go to *File > Build Settings*, switch platform to **iOS**.
- Click **Build**, then open the `.xcodeproj` in Xcode.
- Build and run on a connected iOS device.

---

## 📲 Usage

1. **Scan a Flashcard Image**: Point your device at one of the tracked cell images.
2. **Show Fact**: Tap the **Show Fact** button to display information about the cell.
3. **Take Quiz**: Tap **Take Quiz** to test your knowledge.
4. **Start New Session**: Use **New Session** to clear the screen and reset.

---

## 🧩 Troubleshooting

- **Fact Not Showing?**  
  Ensure `FactTextNew` is visible, has a font assigned, and is positioned above buttons.

- **No AR Response?**  
  Confirm that ARKit and image reference library are configured properly.

- **UI Misalignment?**  
  Adjust `FactTextNew` in the Scene view and ensure proper anchoring and scaling.

---

## 📄 License  
This project is for educational use. You may modify and share with attribution to the original author.

---

## 📬 Contact  
For feedback, bugs, or questions, contact the developer email.
