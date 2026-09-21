# HR Management Release Checklist

Checklist này dùng cho mọi bản phát hành chính thức hoặc release candidate.

Ví dụ:

- 1.0.0-rc2
- 1.0.0
- 1.0.1
- 1.1.0

---

## 1. Source control

- [ ] Đang ở đúng branch phát hành.
- [ ] `git status` sạch.
- [ ] Đã pull/fetch các thay đổi mới nhất.
- [ ] Không còn file build output hoặc dữ liệu local bị stage nhầm.

Kiểm tra:

```bash
git status
git log -1 --oneline