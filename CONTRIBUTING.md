# Contributing to DATN Co-op

> **LƯU Ý QUAN TRỌNG:** Đây là tài liệu quy chuẩn kỹ thuật **BẮT BUỘC** tuân thủ đối với tất cả thành viên tham gia phát triển dự án này. Mọi vi phạm quy tắc trong tài liệu này có thể dẫn đến việc yêu cầu chỉnh sửa lại toàn bộ mã nguồn liên quan.

---

## Mục lục
1. [Quy trình báo cáo vấn đề (Issues)](#1-quy-trình-báo-cáo-vấn-đề-issues)
2. [Quy trình gửi Pull Request (PR)](#2-quy-trình-gửi-pull-request-pr)
3. [Quy tắc đặt tên nhánh (Branch Naming)](#3-quy-tắc-đặt-tên-nhánh-branch-naming)
4. [Quy tắc viết Commit (Commit Messages)](#4-quy-tắc-viết-commit-commit-messages)
5. [Quản lý đồng bộ qua Prefab & Core Systems](#5-quản-lý-đồng-bộ-qua-prefab--core-systems)
6. [Quy chuẩn mã nguồn (Coding Standards)](#6-quy-chuẩn-mã-nguồn-coding-standards)
7. [Cấu trúc thư mục chi tiết (Directory Structure)](#7-cấu-trúc-thư-mục-chi-tiết-directory-structure)
8. [Lưu ý khi làm việc với Unity](#8-lưu-ý-khi-làm-việc-với-unity)
9. [Quy trình Revert, Reapply & Discard (GitHub Desktop)](#9-quy-trình-revert-reapply--discard)
10. [Tài liệu tham khảo (References)](#10-tài-liệu-tham-khảo-references)

---

## 1. Quy trình báo cáo vấn đề (Issues)
Trước khi bắt đầu sửa lỗi hoặc thêm tính năng, hãy tạo một **Issue** trên repository:
- **Bug Report:** Mô tả chi tiết lỗi, các bước tái hiện, kết quả mong đợi và kết quả thực tế.
- **Feature Request:** Mô tả tính năng mới và lý do tại sao nó cần thiết cho dự án.

## 2. Quy trình gửi Pull Request (PR)
1. **Tạo nhánh:** Từ `main`.
2. **Thực hiện thay đổi:** Code, thiết kế map, v.v.
3. **Kiểm tra:** Đảm bảo không lỗi, không xung đột (conflict).
4. **Gửi PR:** Mô tả chi tiết thay đổi và liên kết Issue (ví dụ: `Closes #12`).

## 3. Quy tắc đặt tên nhánh (Branch Naming)
**YÊU CẦU:** Tên nhánh phải được viết bằng **Tiếng Anh (English)**.

Cấu trúc: `type/short-description`

- `feat/ten-tinh-nang`: Thêm tính năng mới.
- `bugfix/ten-loi`: Sửa lỗi.
- `hotfix/loi-nghiem-trong`: Sửa lỗi khẩn cấp.
- `refactor/ten-module`: Tái cấu trúc mã nguồn.
- `docs/ten-tai-lieu`: Cập nhật tài liệu.

**Ví dụ:**
- `feat/add-player-dash`
- `bugfix/fix-network-sync-issue`
- `hotfix/crash-on-startup`
- `refactor/optimize-enemy-ai`
- `docs/update-architecture-doc`

## 4. Quy tắc viết Commit (Commit Messages)
**YÊU CẦU:** Nội dung commit phải được viết bằng **Tiếng Anh (English)** và tuân thủ **Conventional Commits**.

**Định dạng:**
Một commit message bao gồm tiêu đề ngắn và mô tả chi tiết (tùy chọn).
- **Summary:** Giữ dưới 72 ký tự. Sử dụng thì mệnh lệnh.
- **Description:** Mô tả chi tiết qua các gạch đầu dòng. Giải thích *cái gì* đã thay đổi và *tại sao*.

Cấu trúc Summary: `prefix: short description`

- `feat`: Thêm tính năng mới.
- `fix`: Sửa lỗi.
- `refactor`: Thay đổi mã nguồn nhưng không sửa lỗi hay thêm tính năng.
- `docs`: Cập nhật tài liệu.
- `chore`: Các công việc lặt vặt (cập nhật build scripts, thêm thư viện, dọn rác).
- `style`: Thay đổi về format, không ảnh hưởng tới logic.
- `test`: Thêm hoặc sửa các bài test.

**Ví dụ:**
- `feat: implement double jump logic`
- `fix: resolve null reference in PlayerHealth`

**Mẫu Pull request:**
```text
  Branch Name:
  docs/update-contributing-guidelines

  Commit Message:
  docs: update contributing guidelines and add project references

  - Added table of contents and emphasized mandatory compliance.
  - Enforced English for branch names and commit messages.
  - Added guidelines for Prefab and Core system management.
  - Included references to DOCS_ARCH.md and external project resources.
```

## 5. Quản lý đồng bộ qua Prefab & Core Systems
Để đảm bảo tính nhất quán giữa các Scene và các Client trong mạng:
- **= CORE =:** Đây là Prefab chứa các hệ thống cốt lõi (các Manager, EventBus, GameState, v.v.). **KHÔNG** thay đổi trực tiếp trừ khi có sự đồng ý của Lead. Mọi thay đổi phải được kiểm tra kỹ lưỡng vì nó ảnh hưởng đến toàn bộ dự án.
- **GENERAL.prefab:** Đây là Prefab chứa tất cả setup logic chung của game, bao gồm cả = CORE = và Canvas, v.v. Scene game chính và các scene sandbox đều cần Prefab này để hoạt động đồng bộ.
- **Canvas & UI:** Sử dụng các Prefab cho HUD, Menu. Khi cập nhật UI, hãy cập nhật vào bản gốc Prefab thay vì thay đổi trên Scene.
- **Global Systems:** Các hệ thống như `CameraManager`, `SceneLoader` phải luôn được truy cập thông qua `GENERAL` hoặc `EventBus`.

## 6. Quy chuẩn mã nguồn (Coding Standards)
- **C# naming conventions:**
    - `PascalCase`: Classes, Methods, Public Properties.
    - `camelCase`: Local variables.
    - `_camelCase`: Private/Protected fields.
- **Networking:** Sử dụng `[ServerRpc]` và `[ClientRpc]` đúng mục đích. Luôn kiểm tra `IsOwner`, `IsServer`, `IsClient` trước khi thực thi logic quan trọng.

## 7. Cấu trúc thư mục chi tiết (Directory Structure)
```text
D:\GameProjects\DATN_Co-op\
├── Assets\
│   ├── _Game\                      # Chứa toàn bộ logic và tài nguyên dự án
│   │   ├── Animations\             # Animator Controllers, Animation Clips
│   │   ├── Art\                    # Materials, Models, Shaders, Textures
│   │   ├── Audio\                  # Mixer, SFX, Music
│   │   ├── Input\                  # Input Actions, Input Handler
│   │   ├── Prefabs\                # Prefabs hệ thống (GENERAL.prefab, Characters, Environment)
│   │   ├── Scenes\                 # Game scenes (Core, MainMenu, Levels)
│   │   ├── ScriptableObjects\      # Data-driven configs (PlayerConfig, EnemyConfig)
│   │   └── Scripts\                # C# Scripts phân chia theo module
│   │       ├── Core\               # EventBus, StateMachines, SceneLoading
│   │       ├── Player\             # Movement, Combat, States
│   │       ├── Enemies\            # AI Behaviors, Enemy Logic
│   │       ├── Network\            # NGO integration, Syncing, Auth, Relay
│   │       └── UI\                 # HUD, Menu, Dialogues
│   ├── _ThirdParty\                # Thư viện ngoài (Feel, RayFire, Cartoon FX, v.v.)
│   ├── Plugins\                    # Editor plugins (vFolders, vHierarchy, vShortcut) 
│   ├── Settings\                   # URP Settings, Render Pipeline Assets
│   └── TextMesh Pro\               # Tài nguyên TMP
├── ProjectSettings\                # Cấu hình dự án Unity
└── Packages\                       # Package manifest của Unity
```

## 8. Lưu ý khi làm việc với Unity
- **.meta Files:** Tuyệt đối không xóa hoặc bỏ qua các file `.meta`. Chúng quản lý GUID và liên kết giữa các Asset.
- **Scene Work:** Hạn chế làm việc chung trên một Scene lớn. Sử dụng tính năng **Multi-Scene Editing** nếu cần hoặc làm việc trên Prefab.
- **Validation:** Chạy build test định kỳ để đảm bảo tính năng hoạt động trên môi trường đóng gói (Standalone Build).

## 9. Quy trình Revert, Reapply & Discard
Hướng dẫn xử lý các thay đổi mã nguồn an toàn bằng GitHub Desktop:

- **Discard (Hủy bỏ):** Chuột phải vào file trong tab **Changes** -> chọn **Discard changes...**. Dùng để xóa các thay đổi chưa commit. **Thận trọng:** Hành động này không thể hoàn tác.
- **Undo (Hoàn tác commit cục bộ):** Nhấn nút **Undo** ở phía dưới tab **Changes**. Dùng khi vừa commit xong (chưa Push) và muốn sửa lại nội dung commit đó.
- **Revert (Đảo ngược commit đã Push):** Vào tab **History**, chuột phải vào commit gây lỗi -> chọn **Revert changes in commit**. GitHub Desktop sẽ tạo một commit mới `đảo ngược` toàn bộ thay đổi của commit đó.
- **Reapply (Áp dụng lại):** Revert lại chính commit Revert trước đó để đưa tính năng trở lại trạng thái ban đầu một cách sạch sẽ nhất.

**Ví dụ xử lý lỗi từng phần (Partial Revert):**
Giả sử một Pull Request có 2 file thay đổi: **File A** (hoạt động tốt) và **File B** (gây lỗi). Bạn muốn giữ lại thay đổi của File A nhưng loại bỏ File B:
1. Thực hiện **Revert** toàn bộ commit/PR đó (tất cả File A và B đều bị đảo ngược).
2. Nhấn nút **Undo** ở phía dưới tab **Changes** để các thay đổi đảo ngược hiện lên.
3. Thực hiện **Discard changes** **File A** để giữ nguyên tính năng mới, chỉ giữ lại change revert **File B** để đảo ngược thành phần gây lỗi. 
=> Kết quả: File B bị đảo ngược về trạng thái trước lúc commit (loại bỏ lỗi), trong khi File A vẫn giữ tính năng mới (sau commit).

## 10. Tài liệu tham khảo (References)
- **Kiến trúc hệ thống cơ bản:** Xem tệp [DOCS_ARCH.md](./DOCS_ARCH.md) tại thư mục gốc để nắm vững luồng xử lý và các EventBus signals.
- **Tài liệu cơ sở (Google Drive):** [DATN_Co-op Resources](https://drive.google.com/drive/folders/1JeRYYWAEI-OGims3ZYcrIyrYehzhNitG) - Bao gồm SRS, thiết kế game và các tài liệu liên quan khác.

---
Mọi chi tiết thắc mắc vui lòng liên hệ [lphthuan](https://github.com/lphthuan) hoặc leader [JohnnyDat06](https://github.com/JohnnyDat06).

Cảm ơn bạn đã tuân thủ các quy tắc để dự án phát triển bền vững!
