import os
import json
import sys
import threading
import queue
import shutil
import time
import ftplib
import urllib.parse
from dataclasses import dataclass
from concurrent.futures import ThreadPoolExecutor

import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from tkinter.scrolledtext import ScrolledText


SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp", ".mp4", ".exe", ".mp3"}
ALL_FILES_TOKEN = "*.*"  # Seçiliyse tüm dosyaları dahil et
EXT_OPTIONS = [ALL_FILES_TOKEN] + sorted(SUPPORTED_EXTENSIONS)


@dataclass
class CopyStats:
    total_files: int = 0
    total_bytes: int = 0
    copied_files: int = 0
    copied_bytes: int = 0
    skipped_files: int = 0
    errors: int = 0


class ImageCopyApp(tk.Tk):
    def _test_connection(self):
        protocol = self.protocol.get().strip().upper()
        host = self.dst_dir.get().strip()
        user = self.ftp_user.get().strip()
        password = self.ftp_pass.get()
        msg = ""
        error_detail = ""
        import socket
        import urllib.parse
        self._log(f"Bağlantı testi başlatıldı: {protocol}")
        self.lst_errors.delete(0, tk.END)
        try:
            if protocol == "FTP":
                import ftplib
                url = urllib.parse.urlparse(host)
                ftp_host = url.hostname or host
                ftp_port = url.port or 21
                ftp_base = url.path or ''
                ftp = ftplib.FTP()
                ftp.connect(ftp_host, ftp_port, timeout=10)
                ftp.login(user, password)
                if ftp_base:
                    ftp.cwd(ftp_base)
                msg = f"FTP bağlantısı başarılı: {ftp_host}:{ftp_port}"
                ftp.quit()
            elif protocol == "FTPS":
                from ftplib import FTP_TLS
                url = urllib.parse.urlparse(host)
                ftps_host = url.hostname or host
                ftps_port = url.port or 21
                ftps_base = url.path or ''
                ftps = FTP_TLS()
                ftps.connect(ftps_host, ftps_port, timeout=10)
                ftps.auth()
                ftps.prot_p()
                ftps.login(user, password)
                if ftps_base:
                    ftps.cwd(ftps_base)
                msg = f"FTPS bağlantısı başarılı: {ftps_host}:{ftps_port}"
                ftps.quit()
            elif protocol == "SFTP":
                import paramiko
                url = urllib.parse.urlparse(host)
                sftp_host = url.hostname or host
                sftp_port = url.port or 22
                sftp_base = url.path or ''
                transport = paramiko.Transport((sftp_host, sftp_port))
                transport.connect(username=user, password=password)
                sftp = paramiko.SFTPClient.from_transport(transport)
                if sftp_base:
                    sftp.chdir(sftp_base)
                msg = f"SFTP bağlantısı başarılı: {sftp_host}:{sftp_port}"
                sftp.close()
                transport.close()
            else:
                msg = f"Desteklenmeyen protokol: {protocol}"
        except ftplib.error_perm as e:
            msg = f"Bağlantı hatası: Yetki/izin hatası"
            error_detail = str(e)
        except (socket.timeout, TimeoutError) as e:
            msg = "Bağlantı hatası: Zaman aşımı (timeout)"
            error_detail = str(e)
        except (socket.gaierror, ConnectionRefusedError) as e:
            msg = "Bağlantı hatası: Sunucuya erişilemiyor"
            error_detail = str(e)
        except Exception as e:
            msg = f"Bağlantı hatası: {type(e).__name__}"
            error_detail = str(e)
        self._log(msg)
        if error_detail:
            self._log(f"Detay: {error_detail}")
            self.lst_errors.insert(tk.END, msg)
            self.lst_errors.insert(tk.END, error_detail)
        else:
            self.lst_errors.insert(tk.END, msg)
    def _upload_file_to_sftp(self, src_path: str, remote_path: str, sftp_info: dict, cancel_event: threading.Event, chunk_size: int = 64*1024) -> None:
        """Upload a local file to remote SFTP server at remote_path. Uses paramiko."""
        import paramiko
        transport = None
        sftp = None
        try:
            transport = paramiko.Transport((sftp_info['host'], sftp_info['port']))
            transport.connect(username=sftp_info['user'], password=sftp_info['password'])
            sftp = paramiko.SFTPClient.from_transport(transport)
            # Ensure remote directory exists
            remote_dir = os.path.dirname(remote_path)
            try:
                sftp.chdir(remote_dir)
            except IOError:
                # Directory does not exist, create recursively
                dirs = remote_dir.split('/')
                cur = ''
                for d in dirs:
                    if d:
                        cur = f"{cur}/{d}" if cur else d
                        try:
                            sftp.mkdir(cur)
                        except IOError:
                            pass
                sftp.chdir(remote_dir)
            # Upload file
            with open(src_path, 'rb') as f:
                sftp.putfo(f, remote_path)
        finally:
            if sftp:
                sftp.close()
            if transport:
                transport.close()

    def _upload_file_to_ftps(self, src_path: str, remote_path: str, ftps_info: dict, cancel_event: threading.Event, chunk_size: int = 64*1024) -> None:
        """Upload a local file to remote FTPS server at remote_path. Uses ftplib.FTP_TLS."""
        from ftplib import FTP_TLS
        ftps = FTP_TLS()
        try:
            ftps.connect(ftps_info['host'], ftps_info['port'], timeout=20)
            ftps.auth()
            ftps.prot_p()
            ftps.login(ftps_info['user'], ftps_info['password'])
            # Ensure remote directory exists
            remote_dir = os.path.dirname(remote_path)
            if remote_dir:
                try:
                    ftps.cwd(remote_dir)
                except Exception:
                    parts = remote_dir.split('/')
                    cur = ''
                    for part in parts:
                        if part:
                            cur = f"{cur}/{part}" if cur else part
                            try:
                                ftps.mkd(cur)
                            except Exception:
                                pass
                    ftps.cwd(remote_dir)
            # Upload file
            with open(src_path, 'rb') as f:
                ftps.storbinary(f'STOR {os.path.basename(remote_path)}', f)
        finally:
            try:
                ftps.quit()
            except Exception:
                pass
    def __init__(self):
        super().__init__()
        self.title("Resim Kopyalayıcı")
        self.geometry("820x620")
        self.minsize(760, 560)

        # State
        self.src_dir = tk.StringVar()
        self.dst_dir = tk.StringVar()
        self.protocol = tk.StringVar(value="FTP")
        self.ftp_user = tk.StringVar()
        self.ftp_pass = tk.StringVar()
        # *.* varsayılan kapalı, diğerleri açık
        self.ext_vars = {opt: tk.BooleanVar(value=(False if opt == ALL_FILES_TOKEN else True)) for opt in EXT_OPTIONS}
        self.ext_checkbuttons = {}
        self.overwrite = tk.BooleanVar(value=False)
        self._last_selected_exts = None  # set of extensions used during last count
        self.worker_count = tk.IntVar(value=4)
        self.error_files = []
        self._stats_lock = threading.Lock()
        self.enable_retry = tk.BooleanVar(value=True)
        self.remember_last = tk.BooleanVar(value=True)
        # Seçerek kopyalama için ayrı kaynak/hedef
        self.manual_src_dir = tk.StringVar()
        self.manual_dst_dir = tk.StringVar()
        self.manual_filter = tk.StringVar()  # Filtre textbox
        self._manual_items = {}  # iid -> rel path
        self._manual_all_items = []  # Tüm dosyalar (rel, size, name) - filtreleme için
        self._sort_descending = True  # Sıralama yönü

        self.counted_files = []  # list of relative paths
        self.stats = CopyStats()

        self._worker_thread = None
        self._cancel_event = threading.Event()
        self._pause_event = threading.Event()
        self._ui_queue = queue.Queue()
        self._copy_start_time = None
        self._last_speed_update = None
        self._last_copied_bytes = 0
        self._speed = 0.0
        self._eta = ""

        # Config yükle (son klasörleri ve tercihi uygula)
        self._load_config()

        self._build_ui()
        self._poll_queue()

    # ---------------- UI -----------------
    def _build_ui(self):
        pad = {"padx": 10, "pady": 6}

        frm_top = ttk.Frame(self)
        frm_top.pack(fill=tk.X, **pad)

        # Kaynak
        ttk.Label(frm_top, text="Kaynak klasör:").grid(row=0, column=0, sticky="w")
        ent_src = ttk.Entry(frm_top, textvariable=self.src_dir)
        ent_src.grid(row=0, column=1, sticky="ew", padx=(8, 8))
        btn_src = ttk.Button(frm_top, text="Seç…", command=self._choose_src)
        btn_src.grid(row=0, column=2)

        # Hedef
        ttk.Label(frm_top, text="Hedef klasör:").grid(row=1, column=0, sticky="w")
        ent_dst = ttk.Entry(frm_top, textvariable=self.dst_dir)
        ent_dst.grid(row=1, column=1, sticky="ew", padx=(8, 8))
        btn_dst = ttk.Button(frm_top, text="Seç…", command=self._choose_dst)
        btn_dst.grid(row=1, column=2)

        # Protokol ve credential alanları
        frm_ftp = ttk.Frame(frm_top)
        frm_ftp.grid(row=2, column=1, columnspan=2, sticky="ew", pady=(6,0))
        ttk.Label(frm_ftp, text="Protokol:").pack(side=tk.LEFT)
        cmb_proto = ttk.Combobox(frm_ftp, textvariable=self.protocol, values=["FTP", "FTPS", "SFTP"], width=7, state="readonly")
        cmb_proto.pack(side=tk.LEFT, padx=(6,8))
        ttk.Label(frm_ftp, text="Kullanıcı:").pack(side=tk.LEFT)
        ttk.Entry(frm_ftp, textvariable=self.ftp_user, width=14).pack(side=tk.LEFT, padx=(6,8))
        ttk.Label(frm_ftp, text="Parola:").pack(side=tk.LEFT)
        ttk.Entry(frm_ftp, textvariable=self.ftp_pass, show='*', width=14).pack(side=tk.LEFT, padx=(6,0))
        # Bağlantıyı Test Et butonu
        btn_test = ttk.Button(frm_ftp, text="Bağlantıyı Test Et", command=self._test_connection)
        btn_test.pack(side=tk.LEFT, padx=(12,0))

        frm_top.columnconfigure(1, weight=1)

        # Seçenekler
        frm_opts = ttk.LabelFrame(self, text="Filtreler ve seçenekler")
        frm_opts.pack(fill=tk.X, **pad)

        # Uzantılar
        ext_frame = ttk.Frame(frm_opts)
        ext_frame.pack(fill=tk.X, padx=10, pady=6)
        ttk.Label(ext_frame, text="Dahil edilecek uzantılar:").pack(side=tk.LEFT)
        for ext, var in self.ext_vars.items():
            label = f"{ext}"
            if ext == ALL_FILES_TOKEN:
                cb = ttk.Checkbutton(ext_frame, text=label, variable=var, command=self._on_all_ext_toggle)
            else:
                cb = ttk.Checkbutton(ext_frame, text=label, variable=var, command=(lambda e=ext: self._on_non_all_ext_toggle(e)))
            cb.pack(side=tk.LEFT, padx=6)
            self.ext_checkbuttons[ext] = cb

        ttk.Checkbutton(frm_opts, text="Var olan dosyaların üzerine yaz (overwrite)", variable=self.overwrite).pack(anchor="w", padx=10)
        thr_frame = ttk.Frame(frm_opts)
        thr_frame.pack(fill=tk.X, padx=10, pady=6)
        ttk.Label(thr_frame, text="Eşzamanlı işçi sayısı:").pack(side=tk.LEFT)
        spn = ttk.Spinbox(thr_frame, from_=1, to=64, textvariable=self.worker_count, width=5)
        spn.pack(side=tk.LEFT, padx=6)

        retry_frame = ttk.Frame(frm_opts)
        retry_frame.pack(fill=tk.X, padx=10, pady=(0, 10))
        ttk.Checkbutton(retry_frame, text="Hata halinde 15 saniye boyunca yeniden dene", variable=self.enable_retry).pack(side=tk.LEFT)

        # Hatırla seçeneği
        remember_frame = ttk.Frame(frm_opts)
        remember_frame.pack(fill=tk.X, padx=10, pady=(0, 10))
        chk = ttk.Checkbutton(remember_frame, text="Son seçilen klasörleri hatırla", variable=self.remember_last, command=self._save_config)
        chk.pack(side=tk.LEFT)

        # Say ve başlat butonları
        frm_actions = ttk.Frame(self)
        frm_actions.pack(fill=tk.X, **pad)
        self.btn_count = ttk.Button(frm_actions, text="Dosyaları Say", command=self._count_files)
        self.btn_count.pack(side=tk.LEFT)
        self.btn_start = ttk.Button(frm_actions, text="Kopyalamayı Başlat", command=self._start_copy, state=tk.DISABLED)
        self.btn_start.pack(side=tk.LEFT, padx=(10, 0))
        self.btn_pause = ttk.Button(frm_actions, text="Duraklat", command=self._pause_copy, state=tk.DISABLED)
        self.btn_pause.pack(side=tk.LEFT, padx=(10, 0))
        self.btn_resume = ttk.Button(frm_actions, text="Devam Et", command=self._resume_copy, state=tk.DISABLED)
        self.btn_resume.pack(side=tk.LEFT, padx=(10, 0))
        self.btn_stop = ttk.Button(frm_actions, text="Durdur", command=self._cancel_copy, state=tk.DISABLED)
        self.btn_stop.pack(side=tk.LEFT, padx=(10, 0))

        # Başlangıçta *.* seçimine göre diğerlerini etkin/pasif yap
        self._on_all_ext_toggle()

        # İlerleme
        frm_progress = ttk.LabelFrame(self, text="İlerleme")
        frm_progress.pack(fill=tk.X, **pad)
        self.prg = ttk.Progressbar(frm_progress, orient=tk.HORIZONTAL, mode="determinate")
        self.prg.pack(fill=tk.X, padx=10, pady=6)
        self.lbl_prog = ttk.Label(frm_progress, text="Henüz başlatılmadı")
        self.lbl_prog.pack(anchor="w", padx=10, pady=(0, 2))
        self.lbl_speed = ttk.Label(frm_progress, text="Hız: - | Kalan: -")
        self.lbl_speed.pack(anchor="w", padx=10, pady=(0, 6))

        # Log ve Hata listesi için sekmeler
        nb = ttk.Notebook(self)
        nb.pack(fill=tk.BOTH, expand=True, **pad)

        tab_log = ttk.Frame(nb)
        tab_err = ttk.Frame(nb)
        tab_manual = ttk.Frame(nb)
        nb.add(tab_log, text="Log")
        nb.add(tab_err, text="Hatalar")
        nb.add(tab_manual, text="Seçerek Kopyalama")

        # Log alanı
        self.txt_log = ScrolledText(tab_log, height=14, state=tk.NORMAL)
        self.txt_log.pack(fill=tk.BOTH, expand=True, padx=10, pady=6)

        # Hata listesi
        err_top = ttk.Frame(tab_err)
        err_top.pack(fill=tk.X, padx=10, pady=(10, 0))
        ttk.Button(err_top, text="Hata listesini kaydet…", command=self._save_error_list).pack(side=tk.RIGHT)
        self.lst_errors = tk.Listbox(tab_err, height=12)
        self.lst_errors.pack(fill=tk.BOTH, expand=True, padx=10, pady=6)

        # Seçerek kopyalama sekmesi
        man_top = ttk.Frame(tab_manual)
        man_top.pack(fill=tk.X, padx=10, pady=(10, 6))

        ttk.Label(man_top, text="Kaynak:").grid(row=0, column=0, sticky="w")
        ent_msrc = ttk.Entry(man_top, textvariable=self.manual_src_dir)
        ent_msrc.grid(row=0, column=1, sticky="ew", padx=(6, 6))
        ttk.Button(man_top, text="Seç…", command=self._manual_choose_src).grid(row=0, column=2)

        ttk.Label(man_top, text="Hedef:").grid(row=1, column=0, sticky="w")
        ent_mdst = ttk.Entry(man_top, textvariable=self.manual_dst_dir)
        ent_mdst.grid(row=1, column=1, sticky="ew", padx=(6, 6))
        ttk.Button(man_top, text="Seç…", command=self._manual_choose_dst).grid(row=1, column=2)

        man_top.columnconfigure(1, weight=1)

        man_actions = ttk.Frame(tab_manual)
        man_actions.pack(fill=tk.X, padx=10, pady=(0, 6))
        ttk.Button(man_actions, text="Dosyaları Listele", command=self._manual_list_files).pack(side=tk.LEFT)
        ttk.Button(man_actions, text="Seçilenleri Kopyala", command=self._manual_copy_selected).pack(side=tk.LEFT, padx=(10, 0))
        ttk.Button(man_actions, text="Durdur", command=self._cancel_copy).pack(side=tk.LEFT, padx=(10, 0))
        ttk.Button(man_actions, text="Hedefte Eksik Dosyalar", command=self._show_missing_files).pack(side=tk.LEFT, padx=(10, 0))

        # Alt klasörler paneli (çoklu klasör seçme)
        man_folders = ttk.Frame(tab_manual)
        man_folders.pack(fill=tk.BOTH, expand=False, padx=10, pady=(6, 6))
        ttk.Label(man_folders, text="Alt klasörler (çoklu seç):").pack(anchor="w")
        folder_list_frame = ttk.Frame(man_folders)
        folder_list_frame.pack(fill=tk.BOTH, expand=True)
        self.lst_subfolders = tk.Listbox(folder_list_frame, selectmode="extended", height=6)
        self.lst_subfolders.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scr = ttk.Scrollbar(folder_list_frame, orient=tk.VERTICAL, command=self.lst_subfolders.yview)
        scr.pack(side=tk.LEFT, fill=tk.Y)
        self.lst_subfolders.config(yscrollcommand=scr.set)

        folder_btns = ttk.Frame(man_folders)
        folder_btns.pack(fill=tk.X, pady=(6, 0))
        ttk.Button(folder_btns, text="Alt Klasörleri Listele", command=self._list_subfolders).pack(side=tk.LEFT)
        ttk.Button(folder_btns, text="Seçilen Klasörleri Kopyala", command=self._copy_selected_subfolders).pack(side=tk.LEFT, padx=(8,0))

        # Filtre alanı
        man_filter = ttk.Frame(tab_manual)
        man_filter.pack(fill=tk.X, padx=10, pady=(0, 6))
        ttk.Label(man_filter, text="Filtre:").pack(side=tk.LEFT)
        filter_entry = ttk.Entry(man_filter, textvariable=self.manual_filter, width=30)
        filter_entry.pack(side=tk.LEFT, padx=(6, 10))
        filter_entry.bind('<KeyRelease>', self._on_manual_filter_change)
        ttk.Button(man_filter, text="Temizle", command=self._clear_manual_filter).pack(side=tk.LEFT)

        man_list = ttk.Frame(tab_manual)
        man_list.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))
        cols = ("name", "relpath", "size")
        self.tv_files = ttk.Treeview(man_list, columns=cols, show="headings", selectmode="extended")
        self.tv_files.heading("name", text="Ad")
        self.tv_files.heading("relpath", text="Göreli Yol")
        self.tv_files.heading("size", text="Boyut", command=self._sort_by_size)
        self.tv_files.column("name", width=220)
        self.tv_files.column("relpath", width=440)
        self.tv_files.column("size", width=100, anchor="e")
        self.tv_files.pack(fill=tk.BOTH, expand=True)
        
        # Sıralama durumu
        self._sort_descending = True  # Başlangıçta büyükten küçüğe

        # Başlangıçta *.* seçimine göre diğerlerini etkin/pasif yap (butonlar oluşturulduktan sonra)
        self._on_all_ext_toggle()

    # --------------- Helpers --------------
    def _choose_src(self):
        init = None
        if self.remember_last.get():
            cur = self.src_dir.get().strip()
            if cur and os.path.isdir(cur):
                init = cur
        path = filedialog.askdirectory(title="Kaynak klasörü seçin", initialdir=init) if init else filedialog.askdirectory(title="Kaynak klasörü seçin")
        if path:
            self.src_dir.set(path)
            self._log(f"Kaynak: {path}")
            self._prepare_after_path_change()
            self._save_config()

    # ----- Manual (Seçerek) -----
    def _manual_choose_src(self):
        init = None
        cur = self.manual_src_dir.get().strip()
        if cur and os.path.isdir(cur):
            init = cur
        path = filedialog.askdirectory(title="Seçerek: Kaynak klasörü seçin", initialdir=init) if init else filedialog.askdirectory(title="Seçerek: Kaynak klasörü seçin")
        if path:
            self.manual_src_dir.set(path)
            self._log(f"[Seçerek] Kaynak: {path}")

    def _manual_choose_dst(self):
        init = None
        cur = self.manual_dst_dir.get().strip()
        if cur and os.path.isdir(cur):
            init = cur
        path = filedialog.askdirectory(title="Seçerek: Hedef klasörü seçin", initialdir=init) if init else filedialog.askdirectory(title="Seçerek: Hedef klasörü seçin")
        if path:
            self.manual_dst_dir.set(path)
            self._log(f"[Seçerek] Hedef: {path}")

    def _manual_list_files(self):
        src = self.manual_src_dir.get().strip()
        if not src:
            messagebox.showwarning("Uyarı", "Lütfen Seçerek Kopyalama için bir kaynak klasör seçin.")
            return
        if not os.path.isdir(src):
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return
        
        # Geçerli filtreleri uygula: *.* veya belirli uzantılar
        selected_exts = {e for e, v in self.ext_vars.items() if v.get()}
        all_selected = (ALL_FILES_TOKEN in selected_exts)

        # Tüm dosyaları topla ve _manual_all_items'a kaydet
        self._manual_all_items.clear()
        count = 0
        total_bytes = 0
        for root, _, filenames in os.walk(src):
            for name in filenames:
                ext = os.path.splitext(name)[1].lower()
                if all_selected or ext in selected_exts:
                    full = os.path.join(root, name)
                    rel = os.path.relpath(full, src)
                    try:
                        size = os.path.getsize(full)
                    except OSError:
                        size = 0
                    # Skip files that previously errored and are persisted
                    if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                        continue
                    self._manual_all_items.append((rel, size, name))
                    count += 1
                    total_bytes += size
        
        self._log(f"[Seçerek] Bulunan: {count} dosya | Toplam {self._fmt_bytes(total_bytes)}")
        
        # Filtreyi uygula ve TreeView'ı güncelle
        self._apply_manual_filter()

    def _list_subfolders(self):
        """List immediate subfolders of `manual_src_dir` into the Listbox for multi-select."""
        src = self.manual_src_dir.get().strip()
        if not src:
            messagebox.showwarning("Uyarı", "Lütfen Seçerek Kopyalama için bir kaynak klasör seçin.")
            return
        if not os.path.isdir(src):
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return

        # Clear existing
        self.lst_subfolders.delete(0, tk.END)
        try:
            # list immediate directories
            items = []
            for name in sorted(os.listdir(src)):
                full = os.path.join(src, name)
                if os.path.isdir(full):
                    # store relative
                    rel = os.path.relpath(full, src)
                    # skip persisted failed folders? Keep listing; filtering happens on file collection
                    items.append(rel)
            for it in items:
                self.lst_subfolders.insert(tk.END, it)
            self._log(f"[Seçerek] Alt klasörler listelendi: {len(items)} klasör")
        except Exception as e:
            messagebox.showerror("Hata", f"Alt klasörler listelenemedi: {e}")

    def _copy_selected_subfolders(self):
        """Copy files from selected subfolders under manual_src_dir to manual_dst_dir respecting filters."""
        src = self.manual_src_dir.get().strip()
        dst = self.manual_dst_dir.get().strip()
        if not src or not dst:
            messagebox.showwarning("Uyarı", "Lütfen Seçerek Kopyalama için kaynak ve hedef klasörleri seçin.")
            return
        if not os.path.isdir(src):
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return
        # Eğer hedef ftp:// ile başlıyorsa local klasör oluşturma!
        if not dst.lower().startswith('ftp://'):
            if not os.path.isdir(dst):
                try:
                    os.makedirs(dst, exist_ok=True)
                except Exception as e:
                    messagebox.showerror("Hata", f"Hedef klasör oluşturulamadı: {e}")
                    return

        # Prevent copying into itself (skip this check for FTP destinations)
        if not dst.lower().startswith('ftp://'):
            src_abs = os.path.abspath(src)
            dst_abs = os.path.abspath(dst)
            try:
                common = os.path.commonpath([src_abs, dst_abs])
            except Exception:
                common = ""
            if src_abs == dst_abs or common == src_abs:
                messagebox.showerror("Hata", "Hedef klasör, kaynak klasör ile aynı olamaz veya onun içinde olamaz.")
                return

        sel = self.lst_subfolders.curselection()
        if not sel:
            messagebox.showinfo("Bilgi", "Kopyalamak için listeden en az bir alt klasör seçin.")
            return

        # Build file list from selected folders
        selected_exts = {e for e, v in self.ext_vars.items() if v.get()}
        all_selected = (ALL_FILES_TOKEN in selected_exts)

        files = []
        total_bytes = 0
        for idx in sel:
            rel_folder = self.lst_subfolders.get(idx)
            full_folder = os.path.join(src, rel_folder)
            for root, _, filenames in os.walk(full_folder):
                for name in filenames:
                    ext = os.path.splitext(name)[1].lower()
                    if all_selected or ext in selected_exts:
                        full = os.path.join(root, name)
                        rel = os.path.relpath(full, src)
                        # skip persisted failed files
                        if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                            continue
                        files.append(rel)
                        try:
                            total_bytes += os.path.getsize(full)
                        except OSError:
                            pass

        if not files:
            messagebox.showinfo("Bilgi", "Seçilen klasörlerde kopyalanacak dosya bulunamadı.")
            return

        # Prepare stats and start using existing copy pipeline
        self.counted_files = list(files)
        self.stats = CopyStats(total_files=len(files), total_bytes=total_bytes)
        self.prg.configure(value=0, maximum=max(1, self.stats.total_files))
        self.lbl_prog.configure(text=self._format_status())
        self._clear_errors()
        self._log(f"[Seçilen Klasörler] Kopyalanacak: {len(files)} dosya | Toplam {self._fmt_bytes(total_bytes)}")
        self._begin_copy_with_known_list(src, dst, files)

    def _manual_copy_selected(self):
        src = self.manual_src_dir.get().strip()
        dst = self.manual_dst_dir.get().strip()
        if not src or not dst:
            messagebox.showwarning("Uyarı", "Lütfen Seçerek Kopyalama için kaynak ve hedef klasörleri seçin.")
            return
        if not os.path.isdir(src):
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return
        # Eğer hedef ftp:// ile başlıyorsa local klasör oluşturma!
        if not dst.lower().startswith('ftp://'):
            if not os.path.isdir(dst):
                try:
                    os.makedirs(dst, exist_ok=True)
                except Exception as e:
                    messagebox.showerror("Hata", f"Hedef klasör oluşturulamadı: {e}")
                    return

        # Kaynak ile hedefin çakışmasını engelle (FTP hedeflerinde bu kontrolü atla)
        if not dst.lower().startswith('ftp://'):
            src_abs = os.path.abspath(src)
            dst_abs = os.path.abspath(dst)
            try:
                common = os.path.commonpath([src_abs, dst_abs])
            except Exception:
                common = ""
            if src_abs == dst_abs or common == src_abs:
                messagebox.showerror("Hata", "Hedef klasör, kaynak klasör ile aynı olamaz veya onun içinde olamaz.")
                return

        sel = self.tv_files.selection()
        if not sel:
            messagebox.showinfo("Bilgi", "Kopyalamak için listeden en az bir dosya seçin.")
            return

        files = []
        total_bytes = 0
        for iid in sel:
            rel, size = self._manual_items.get(iid, (None, 0))
            if rel:
                files.append(rel)
                total_bytes += size
        if not files:
            messagebox.showwarning("Uyarı", "Seçilen öğeler geçersiz.")
            return

        # Sayım ve istatistikleri hazırla, ardından mevcut kopyalama mekanizmasını kullan
        self.counted_files = list(files)
        self.stats = CopyStats(total_files=len(files), total_bytes=total_bytes)
        self.prg.configure(value=0, maximum=max(1, self.stats.total_files))
        self.lbl_prog.configure(text=self._format_status())
        self._clear_errors()
        self._log(f"[Seçerek] Kopyalanacak: {len(files)} dosya | Toplam {self._fmt_bytes(total_bytes)}")
        self._begin_copy_with_known_list(src, dst, files)

    def _apply_manual_filter(self):
        """Mevcut filtre textine göre TreeView'ı günceller"""
        # Mevcut listeyi temizle
        for iid in self.tv_files.get_children():
            self.tv_files.delete(iid)
        self._manual_items.clear()

        filter_text = self.manual_filter.get().strip().lower()
        
        displayed_count = 0
        displayed_bytes = 0
        
        for rel, size, name in self._manual_all_items:
            # Filtre kontrolü: dosya adı veya yolda arama
            if not filter_text or filter_text in name.lower() or filter_text in rel.lower():
                iid = self.tv_files.insert("", tk.END, values=(name, rel, self._fmt_bytes(size)))
                self._manual_items[iid] = (rel, size)
                displayed_count += 1
                displayed_bytes += size
        
        if filter_text:
            self._log(f"[Seçerek] Filtre '{filter_text}': {displayed_count} dosya gösteriliyor | {self._fmt_bytes(displayed_bytes)}")

    def _on_manual_filter_change(self, event=None):
        """Filtre textbox değiştiğinde çağrılır"""
        if hasattr(self, '_manual_all_items') and self._manual_all_items:
            self._apply_manual_filter()

    def _clear_manual_filter(self):
        """Filtreyi temizler ve tüm dosyaları gösterir"""
        self.manual_filter.set("")
        if hasattr(self, '_manual_all_items') and self._manual_all_items:
            self._apply_manual_filter()

    def _sort_by_size(self):
        """Boyut sütununa tıklandığında dosyaları boyuta göre sıralar"""
        if not hasattr(self, '_manual_all_items') or not self._manual_all_items:
            return
        
        # Sıralama yönünü değiştir
        self._sort_descending = not self._sort_descending
        
        # Mevcut filtrelenmiş öğeleri al
        current_items = []
        for iid in self.tv_files.get_children():
            rel, size = self._manual_items[iid]
            name = self.tv_files.item(iid)['values'][0]
            current_items.append((rel, size, name))
        
        # Boyuta göre sırala
        current_items.sort(key=lambda x: x[1], reverse=self._sort_descending)
        
        # TreeView'ı temizle ve sıralı olarak yeniden ekle
        for iid in self.tv_files.get_children():
            self.tv_files.delete(iid)
        self._manual_items.clear()
        
        for rel, size, name in current_items:
            iid = self.tv_files.insert("", tk.END, values=(name, rel, self._fmt_bytes(size)))
            self._manual_items[iid] = (rel, size)
        
        # Başlık güncelle
        arrow = "↓" if self._sort_descending else "↑"
        self.tv_files.heading("size", text=f"Boyut {arrow}")

    def _show_missing_files(self):
        """Hedef klasöründe olmayan (kaynak klasöründe eksik) dosyaları gösterir"""
        src = self.manual_src_dir.get().strip()
        dst = self.manual_dst_dir.get().strip()
        
        if not src or not dst:
            messagebox.showwarning("Uyarı", "Lütfen hem kaynak hem hedef klasör seçin.")
            return
        # If destination is FTP URL, this feature (checking missing files locally) is not supported
        if dst.lower().startswith('ftp://'):
            messagebox.showinfo("Bilgi", "Hedefte Eksik Dosyalar özelliği FTP hedefleri için desteklenmiyor.")
            return
        if not os.path.isdir(src) or not os.path.isdir(dst):
            messagebox.showerror("Hata", "Geçersiz kaynak veya hedef klasör.")
            return
        
        # Hedef klasördeki dosyaları topla
        dst_files = set()
        selected_exts = {e for e, v in self.ext_vars.items() if v.get()}
        all_selected = (ALL_FILES_TOKEN in selected_exts)
        
        for root, _, filenames in os.walk(dst):
            for name in filenames:
                ext = os.path.splitext(name)[1].lower()
                if all_selected or ext in selected_exts:
                    full = os.path.join(root, name)
                    rel = os.path.relpath(full, dst)
                    dst_files.add(rel)
        
        # Kaynak klasördeki dosyaları kontrol et - hedefte olmayan dosyaları bul
        missing_files = []
        for root, _, filenames in os.walk(src):
            for name in filenames:
                ext = os.path.splitext(name)[1].lower()
                if all_selected or ext in selected_exts:
                    full = os.path.join(root, name)
                    rel = os.path.relpath(full, src)
                    # Skip files that previously errored and are persisted
                    if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                        continue
                    if rel not in dst_files:
                        try:
                            size = os.path.getsize(full)
                        except OSError:
                            size = 0
                        missing_files.append((rel, size, name))
        
        if not missing_files:
            messagebox.showinfo("Bilgi", "Tüm dosyalar hedef klasörde mevcut!\nHiç eksik dosya bulunamadı.")
            return
        
        # Eksik dosyaları _manual_all_items'a kaydet ve göster
        self._manual_all_items = missing_files
        self._apply_manual_filter()
        
        total_missing_size = sum(item[1] for item in missing_files)
        self._log(f"[Eksik Dosyalar] Hedefte bulunamayan: {len(missing_files)} dosya | Toplam {self._fmt_bytes(total_missing_size)}")
        messagebox.showinfo("Eksik Dosyalar", 
                          f"Hedef klasöründe olmayan {len(missing_files)} dosya bulundu.\n"
                          f"Toplam boyut: {self._fmt_bytes(total_missing_size)}\n\n"
                          f"Bu dosyalar listede gösteriliyor.")

    def _choose_dst(self):
        init = None
        if self.remember_last.get():
            cur = self.dst_dir.get().strip()
            if cur and os.path.isdir(cur):
                init = cur
        path = filedialog.askdirectory(title="Hedef klasörü seçin", initialdir=init) if init else filedialog.askdirectory(title="Hedef klasörü seçin")
        if path:
            self.dst_dir.set(path)
            self._log(f"Hedef: {path}")
            self._prepare_after_path_change()
            self._save_config()

    def _prepare_after_path_change(self):
        self.counted_files = []
        self.stats = CopyStats()
        self.prg.configure(value=0, maximum=100)
        self.lbl_prog.configure(text="Henüz başlatılmadı")
        self.btn_start.configure(state=tk.DISABLED)
        self._clear_errors()

    def _on_all_ext_toggle(self):
        # *.* seçiliyse diğer uzantıları pasifleştir ve seçili hale getir (gerekli değil ama tutarlılık için)
        all_on = self.ext_vars.get(ALL_FILES_TOKEN, tk.BooleanVar(value=False)).get()
        for ext, cb in self.ext_checkbuttons.items():
            if ext == ALL_FILES_TOKEN:
                continue
            if all_on:
                cb.state(["disabled"])  # ttk style
            else:
                cb.state(["!disabled"])  # enable
        # Start butonunu durumla tutarlı yapalım
        self.btn_start.configure(state=(tk.NORMAL if self.dst_dir.get().strip() else tk.DISABLED))

    def _on_non_all_ext_toggle(self, ext: str):
        # Herhangi bir özgül uzantı işaretlenirse *.*'i kaldır
        if ext != ALL_FILES_TOKEN and self.ext_vars.get(ALL_FILES_TOKEN).get():
            self.ext_vars[ALL_FILES_TOKEN].set(False)
            self._on_all_ext_toggle()

    def _log(self, msg: str):
        ts = time.strftime("%H:%M:%S")
        self.txt_log.insert(tk.END, f"[{ts}] {msg}\n")
        self.txt_log.see(tk.END)

    def _poll_queue(self):
        try:
            while True:
                item = self._ui_queue.get_nowait()
                self._handle_ui_event(item)
        except queue.Empty:
            pass
        self.after(100, self._poll_queue)

    def _handle_ui_event(self, item):
        etype = item.get("type")
        if etype == "count_done":
            self._on_count_done(item)
        elif etype == "progress":
            self._on_progress(item)
        elif etype == "done":
            self._on_done(item)
        elif etype == "log":
            self._log(item.get("msg", ""))
        # (previously: ask_permission_error) Permission errors are now handled automatically
        elif etype == "start_after_count":
            # Başlatma isteği sayımdan sonra gelmişse kopyalamayı başlat
            src = item.get("src")
            dst = item.get("dst")
            if self.counted_files:
                self._begin_copy_with_known_list(src, dst, list(self.counted_files))
        elif etype == "add_error":
            rel = item.get("rel")
            if rel:
                self._add_error(rel)

    # --------------- Counting --------------
    def _count_files(self):        
        src = self.src_dir.get().strip()
        if not src:
            messagebox.showwarning("Uyarı", "Lütfen bir kaynak klasör seçin.")
            return
        if not os.path.isdir(src):            
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return

        selected_exts = {e for e, v in self.ext_vars.items() if v.get()}
        if not selected_exts:
            messagebox.showwarning("Uyarı", "En az bir uzantı seçmelisiniz.")
            return

        self.btn_count.configure(state=tk.DISABLED)
        self.btn_start.configure(state=tk.DISABLED)
        self._log("Dosyalar sayılıyor…")

        threading.Thread(target=self._count_worker, args=(src, selected_exts), daemon=True).start()
    def _on_count_done(self, item):        
        self.counted_files = item["files"]
        self.stats = CopyStats(total_files=len(self.counted_files), total_bytes=item["total_bytes"])
        self.prg.configure(value=0, maximum=max(1, self.stats.total_files))
        self.lbl_prog.configure(text=self._format_status())
        self._log(f"Bulunan dosya: {self.stats.total_files} | Toplam boyut: {self._fmt_bytes(self.stats.total_bytes)}")
        self.btn_count.configure(state=tk.NORMAL)
        self.btn_start.configure(state=(tk.NORMAL if self.stats.total_files > 0 and self.dst_dir.get().strip() else tk.DISABLED))
        # Güncel seçili uzantıları kaydet
        self._last_selected_exts = {e for e, v in self.ext_vars.items() if v.get()}

    def _on_progress(self, item):
        # İlerleme güncellemesi için basit bir metot
        progress = item.get("progress", 0)
        total = item.get("total", 1)
        self.prg.configure(value=progress, maximum=max(1, total))
        self.lbl_prog.configure(text=f"İlerleme: {progress}/{total}")
        msg = item.get("msg")
        if msg:
            self._log(msg)

    def _count_worker(self, src: str, selected_exts: set):
        files = []
        total_bytes = 0
        all_selected = (ALL_FILES_TOKEN in selected_exts)
        for root, _, filenames in os.walk(src):
            for name in filenames:
                ext = os.path.splitext(name)[1].lower()
                if all_selected or ext in selected_exts:
                    full = os.path.join(root, name)
                    rel = os.path.relpath(full, src)
                    # Skip files that previously errored and are persisted
                    if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                        continue
                    files.append(rel)
                    try:
                        total_bytes += os.path.getsize(full)
                    except OSError:
                        pass
        self._ui_queue.put({
            "type": "count_done",
            "files": files,
            "total_bytes": total_bytes,
        })

    def _format_status(self):
        # Returns a string for the progress label, e.g. "Kopyalanan: X/Y dosya"
        if hasattr(self, 'stats') and self.stats:
            return f"Kopyalanan: {getattr(self.stats, 'copied_files', 0)}/{getattr(self.stats, 'total_files', 0)} dosya"
        return "Henüz başlatılmadı"

    # --------------- Copying --------------
    def _start_copy(self):
        if self._worker_thread and self._worker_thread.is_alive():
            return

        src = self.src_dir.get().strip()
        dst = self.dst_dir.get().strip()
        if not src or not dst:
            messagebox.showwarning("Uyarı", "Lütfen kaynak ve hedef klasörleri seçin.")
            return
        if not os.path.isdir(src):
            messagebox.showerror("Hata", "Geçersiz kaynak klasör.")
            return
        # Eğer hedef ftp:// ile başlıyorsa local klasör oluşturma!
        if not dst.lower().startswith('ftp://'):
            if not os.path.isdir(dst):
                try:
                    os.makedirs(dst, exist_ok=True)
                except Exception as e:
                    messagebox.showerror("Hata", f"Hedef klasör oluşturulamadı: {e}")
                    return

        # Kaynak ile hedefin çakışmasını engelle
        src_abs = os.path.abspath(src)
        dst_abs = os.path.abspath(dst)
        try:
            common = os.path.commonpath([src_abs, dst_abs])
        except Exception:
            common = ""
        if src_abs == dst_abs or common == src_abs:
            messagebox.showerror("Hata", "Hedef klasör, kaynak klasör ile aynı olamaz veya onun içinde olamaz.")
            return

        selected_exts = {e for e, v in self.ext_vars.items() if v.get()}
        # Eğer önceden sayım yapılmadıysa veya filtre değişmişse yeniden hesapla
        if not self.counted_files or (self._last_selected_exts is not None and selected_exts != self._last_selected_exts):
            self._log("Ön sayım yok, dosyalar sayılıyor…")
            self.btn_count.configure(state=tk.DISABLED)
            threading.Thread(target=self._count_and_start, args=(src, selected_exts, dst), daemon=True).start()
            return

        self._begin_copy_with_known_list(src, dst, list(self.counted_files))

    def _count_and_start(self, src: str, selected_exts: set, dst: str):
        files = []
        total_bytes = 0
        for root, _, filenames in os.walk(src):
            for name in filenames:
                ext = os.path.splitext(name)[1].lower()
                if (ALL_FILES_TOKEN in selected_exts) or (ext in selected_exts):
                    full = os.path.join(root, name)
                    rel = os.path.relpath(full, src)
                    # Skip files that previously errored and are persisted
                    if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                        continue
                    files.append(rel)
                    try:
                        total_bytes += os.path.getsize(full)
                    except OSError:
                        pass
        self._ui_queue.put({"type": "count_done", "files": files, "total_bytes": total_bytes})
        # Başlat
        self._ui_queue.put({"type": "log", "msg": "Kopyalama başlatılıyor…"})
        self._ui_queue.put({"type": "start_after_count", "src": src, "dst": dst})

    def _begin_copy_with_known_list(self, src: str, dst: str, files: list):
        self._cancel_event.clear()
        self.stats.copied_files = 0
        self.stats.copied_bytes = 0
        self.stats.skipped_files = 0
        self.stats.errors = 0
        self._clear_errors()

        self.btn_start.configure(state=tk.DISABLED)
        self.btn_stop.configure(state=tk.NORMAL)
        self.btn_count.configure(state=tk.DISABLED)

        overwrite = self.overwrite.get()
        workers = max(1, int(self.worker_count.get() or 1))
        retry_enabled = self.enable_retry.get()

        def do_copy(rel: str):
            if self._cancel_event.is_set():
                # İş iptal ise hızlıca dön
                with self._stats_lock:
                    self.stats.skipped_files += 1
                self._ui_queue.put({"type": "progress"})
                return

            # Skip files that previously failed and are persisted
            if hasattr(self, '_persistent_failed_set') and rel in self._persistent_failed_set:
                self._ui_queue.put({"type": "log", "msg": f"Atlandı (önceden hata): {rel}"})
                with self._stats_lock:
                    self.stats.skipped_files += 1
                self._ui_queue.put({"type": "progress"})
                return

            src_path = os.path.join(src, rel)

            # Protokol seçimine göre upload
            protocol = self.protocol.get().strip().upper()
            remote_rel = rel.replace(os.sep, '/').lstrip('./')
            if isinstance(dst, str) and (dst.strip().lower().startswith('ftp://') or dst.strip().lower().startswith('ftps://') or dst.strip().lower().startswith('sftp://')):
                # Bilgileri hazırla
                if protocol == "FTP":
                    try:
                        ftp_info = self._parse_ftp_url(dst)
                        if not ftp_info.get('user') and getattr(self, 'ftp_user', None):
                            u = self.ftp_user.get().strip()
                            p = self.ftp_pass.get()
                            if u:
                                ftp_info['user'] = u
                                ftp_info['password'] = p
                    except Exception as e:
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata: Geçersiz FTP hedefi: {e}"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
                        self._ui_queue.put({"type": "progress"})
                        return
                    try:
                        self._upload_file_to_ftp(src_path, remote_rel, ftp_info, self._cancel_event)
                        size = os.path.getsize(src_path)
                        with self._stats_lock:
                            self.stats.copied_files += 1
                            self.stats.copied_bytes += size
                        self._ui_queue.put({"type": "log", "msg": f"Kopyalandı (FTP): {rel}"})
                    except InterruptedError:
                        self._ui_queue.put({"type": "log", "msg": f"İptal edildi: {rel} (kopya sırasında)"})
                        with self._stats_lock:
                            self.stats.skipped_files += 1
                    except Exception as e:
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata (FTP): {rel} -> {e}"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
                    finally:
                        self._ui_queue.put({"type": "progress"})
                    return
                elif protocol == "FTPS":
                    # FTPS URL parse
                    url = urllib.parse.urlparse(dst)
                    ftps_info = {
                        'host': url.hostname,
                        'port': url.port or 21,
                        'user': self.ftp_user.get().strip(),
                        'password': self.ftp_pass.get()
                    }
                    try:
                        self._upload_file_to_ftps(src_path, remote_rel, ftps_info, self._cancel_event)
                        size = os.path.getsize(src_path)
                        with self._stats_lock:
                            self.stats.copied_files += 1
                            self.stats.copied_bytes += size
                        self._ui_queue.put({"type": "log", "msg": f"Kopyalandı (FTPS): {rel}"})
                    except InterruptedError:
                        self._ui_queue.put({"type": "log", "msg": f"İptal edildi: {rel} (kopya sırasında)"})
                        with self._stats_lock:
                            self.stats.skipped_files += 1
                    except Exception as e:
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata (FTPS): {rel} -> {e}"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
                    finally:
                        self._ui_queue.put({"type": "progress"})
                    return
                elif protocol == "SFTP":
                    # SFTP URL parse
                    url = urllib.parse.urlparse(dst)
                    sftp_info = {
                        'host': url.hostname,
                        'port': url.port or 22,
                        'user': self.ftp_user.get().strip(),
                        'password': self.ftp_pass.get()
                    }
                    try:
                        self._upload_file_to_sftp(src_path, remote_rel, sftp_info, self._cancel_event)
                        size = os.path.getsize(src_path)
                        with self._stats_lock:
                            self.stats.copied_files += 1
                            self.stats.copied_bytes += size
                        self._ui_queue.put({"type": "log", "msg": f"Kopyalandı (SFTP): {rel}"})
                    except InterruptedError:
                        self._ui_queue.put({"type": "log", "msg": f"İptal edildi: {rel} (kopya sırasında)"})
                        with self._stats_lock:
                            self.stats.skipped_files += 1
                    except Exception as e:
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata (SFTP): {rel} -> {e}"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
                    finally:
                        self._ui_queue.put({"type": "progress"})
                    return

            # Local filesystem copy path
            dst_path = os.path.join(dst, rel)
            dst_dir = os.path.dirname(dst_path)
            try:
                os.makedirs(dst_dir, exist_ok=True)
                if os.path.exists(dst_path) and not overwrite:
                    with self._stats_lock:
                        self.stats.skipped_files += 1
                    self._ui_queue.put({"type": "log", "msg": f"Atlandı (mevcut): {rel}"})
                    return

                try:
                    self._copy_file_chunked(src_path, dst_path, self._cancel_event)
                    size = 0
                    try:
                        size = os.path.getsize(src_path)
                    except OSError:
                        pass
                    with self._stats_lock:
                        self.stats.copied_files += 1
                        self.stats.copied_bytes += size
                    self._ui_queue.put({"type": "log", "msg": f"Kopyalandı: {rel}"})
                except InterruptedError:
                    # Copy was cancelled mid-file
                    self._ui_queue.put({"type": "log", "msg": f"İptal edildi: {rel} (kopya sırasında)"})
                    with self._stats_lock:
                        self.stats.skipped_files += 1
                except Exception as e:
                    # For permission/access errors: automatically persist the file so it's not retried
                    if isinstance(e, PermissionError):
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata (izin): {rel} -> {e} | Dosya hata listesine eklendi"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
                    else:
                        # Other errors: persist by default
                        with self._stats_lock:
                            self.stats.errors += 1
                        self._ui_queue.put({"type": "log", "msg": f"Hata: {rel} -> {e}"})
                        self._ui_queue.put({"type": "add_error", "rel": rel})
                        try:
                            self._add_to_failed_list(rel)
                        except Exception:
                            pass
            finally:
                self._ui_queue.put({"type": "progress"})

        def worker():
            self._copy_start_time = time.time()
            self._last_speed_update = self._copy_start_time
            self._last_copied_bytes = 0
            with ThreadPoolExecutor(max_workers=workers) as ex:
                try:
                    for rel in files:
                        if self._cancel_event.is_set():
                            break
                        ex.submit(do_copy, rel)
                except Exception:
                    pass
                finally:
                    if self._cancel_event.is_set():
                        try:
                            ex.shutdown(wait=False)
                        except Exception:
                            pass
            elapsed = time.time() - self._copy_start_time
            self._ui_queue.put({"type": "done", "canceled": self._cancel_event.is_set(), "elapsed": elapsed})

        self._worker_thread = threading.Thread(target=worker, daemon=True)
        self._worker_thread.start()

    def _pause_copy(self):
        if self._worker_thread and self._worker_thread.is_alive():
            self._pause_event.set()
            self._log("⏸️ Kopyalama duraklatıldı.")
            self.btn_pause.configure(state=tk.DISABLED)
            self.btn_resume.configure(state=tk.NORMAL)

    def _resume_copy(self):
        if self._worker_thread and self._worker_thread.is_alive():
            self._pause_event.clear()
            self._log("▶️ Kopyalama devam ediyor.")
            self.btn_pause.configure(state=tk.NORMAL)
            self.btn_resume.configure(state=tk.DISABLED)

    def _cancel_copy(self):
        if self._worker_thread and self._worker_thread.is_alive():
            self._cancel_event.set()
            self._pause_event.clear()
            self._log("🛑 İPTAL İSTENİYOR - Tüm disk işlemleri durduruluyor...")
            self.btn_stop.configure(state=tk.DISABLED)
            self.btn_pause.configure(state=tk.DISABLED)
            self.btn_resume.configure(state=tk.DISABLED)

    def _on_done(self, item):
        canceled = item.get("canceled", False)
        elapsed = item.get("elapsed", 0.0)
        rapor = (
            f"Kopyalama Raporu:\n"
            f"Toplam dosya: {self.stats.total_files}\n"
            f"Kopyalanan: {self.stats.copied_files}\n"
            f"Atlanan: {self.stats.skipped_files}\n"
            f"Hatalı: {self.stats.errors}\n"
            f"Toplam boyut: {self._fmt_bytes(self.stats.total_bytes)}\n"
            f"Geçen süre: {elapsed:.1f} sn"
        )
        if canceled:
            self._log("Kopyalama iptal edildi.")
        else:
            self._log(f"Tamamlandı. Süre: {elapsed:.1f} sn")
        self._log(rapor)
        try:
            messagebox.showinfo("Kopyalama Raporu", rapor)
        except Exception:
            pass
        self.btn_start.configure(state=tk.NORMAL)
        self.btn_stop.configure(state=tk.DISABLED)
        self.btn_pause.configure(state=tk.DISABLED)
        self.btn_resume.configure(state=tk.DISABLED)
        self.btn_count.configure(state=tk.NORMAL)
        self._on_progress({})

    def _cancel_copy(self):
        if self._worker_thread and self._worker_thread.is_alive():
            self._cancel_event.set()
            self._pause_event.clear()
            self._log("🛑 İPTAL İSTENİYOR - Tüm disk işlemleri durduruluyor...")
            self.btn_stop.configure(state=tk.DISABLED)
            self.btn_pause.configure(state=tk.DISABLED)
            self.btn_resume.configure(state=tk.DISABLED)

    def _clear_errors(self):
        self.error_files = []
        if hasattr(self, 'lst_errors'):
            self.lst_errors.delete(0, tk.END)

    # --------------- Config --------------
    def _config_dir(self) -> str:
        appdata = os.getenv("APPDATA")
        if appdata:
            return os.path.join(appdata, "ResimKopyalayici")
        return os.path.join(os.path.expanduser("~"), ".resim_kopyalayici")

    def _config_path(self) -> str:
        return os.path.join(self._config_dir(), "config.json")

    def _load_config(self):
        try:
            cfg_path = self._config_path()
            if os.path.isfile(cfg_path):
                with open(cfg_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                remember = bool(data.get("remember_last", True))
                last_src = data.get("last_src_dir")
                last_dst = data.get("last_dst_dir")
                self.remember_last.set(remember)
                if remember:
                    if last_src and isinstance(last_src, str):
                        self.src_dir.set(last_src)
                    if last_dst and isinstance(last_dst, str):
                        self.dst_dir.set(last_dst)
                    # load ftp credentials if present
                    ftp_user = data.get("ftp_user")
                    ftp_pass = data.get("ftp_pass")
                    if ftp_user and isinstance(ftp_user, str):
                        self.ftp_user.set(ftp_user)
                    if ftp_pass and isinstance(ftp_pass, str):
                        self.ftp_pass.set(ftp_pass)
        except Exception:
            # Config okuma hatası kritik değil; görmezden gel
            pass

        # Load persistent failed files list (do not retry these)
        try:
            self._persistent_failed_set = set()
            failed_path = self._failed_list_path()
            if os.path.isfile(failed_path):
                with open(failed_path, "r", encoding="utf-8") as f:
                    for line in f:
                        p = line.strip()
                        if p:
                            self._persistent_failed_set.add(p)
        except Exception:
            self._persistent_failed_set = set()
        except Exception:
            # Config okuma hatası kritik değil; görmezden gel
            pass

    def _save_config(self):
        try:
            cfg_dir = self._config_dir()
            os.makedirs(cfg_dir, exist_ok=True)
            data = {
                "remember_last": bool(self.remember_last.get()),
                "last_src_dir": self.src_dir.get().strip() if self.remember_last.get() else "",
                "last_dst_dir": self.dst_dir.get().strip() if self.remember_last.get() else "",
                # store ftp creds when remembering
                "ftp_user": self.ftp_user.get().strip() if self.remember_last.get() else "",
                "ftp_pass": self.ftp_pass.get() if self.remember_last.get() else "",
            }
            with open(self._config_path(), "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
        except Exception:
            # Yazma hatası kritik değil; loga düşebiliriz
            try:
                self._log("Uyarı: Ayarlar kaydedilemedi.")
            except Exception:
                pass

    def _failed_list_path(self) -> str:
        cfg_dir = self._config_dir()
        return os.path.join(cfg_dir, "failed_files.txt")

    def _save_failed_list(self):
        try:
            os.makedirs(self._config_dir(), exist_ok=True)
            with open(self._failed_list_path(), "w", encoding="utf-8") as f:
                for p in sorted(self._persistent_failed_set):
                    f.write(p + "\n")
        except Exception:
            try:
                self._log("Uyarı: Hata listesi kaydedilemedi.")
            except Exception:
                pass

    def _add_to_failed_list(self, rel: str):
        try:
            if not hasattr(self, '_persistent_failed_set'):
                self._persistent_failed_set = set()
            if rel not in self._persistent_failed_set:
                self._persistent_failed_set.add(rel)
                self._save_failed_list()
        except Exception:
            pass

    def _add_error(self, rel: str):
        self.error_files.append(rel)
        if hasattr(self, 'lst_errors'):
            self.lst_errors.insert(tk.END, rel)
        # persist the error so we don't retry this file in future runs
        try:
            self._add_to_failed_list(rel)
        except Exception:
            pass

    def _save_error_list(self):
        if not self.error_files:
            messagebox.showinfo("Bilgi", "Kaydedilecek hata bulunmuyor.")
            return
        path = filedialog.asksaveasfilename(
            title="Hata listesini kaydet",
            defaultextension=".txt",
            filetypes=[("Metin Dosyası", "*.txt"), ("Tüm Dosyalar", "*.*")]
        )
        if not path:
            return
        try:
            with open(path, "w", encoding="utf-8") as f:
                for rel in self.error_files:
                    f.write(rel + "\n")
            messagebox.showinfo("Bilgi", f"Hata listesi kaydedildi:\n{path}")
        except Exception as e:
            messagebox.showerror("Hata", f"Hata listesi kaydedilemedi: {e}")

    @staticmethod
    def _fmt_bytes(n: int) -> str:
        # Human-readable bytes
        step = 1024.0
        for unit in ["B", "KB", "MB", "GB", "TB"]:
            if n < step:
                return f"{n:.0f} {unit}" if unit == "B" else f"{n:.1f} {unit}"
            n /= step
        return f"{n:.1f} PB"

    def _copy_file_chunked(self, src_path: str, dst_path: str, cancel_event: threading.Event, chunk_size: int = 64 * 1024, stall_timeout: float = 60.0) -> None:
        """
        Copy a file in chunks, checking cancel_event between chunks so copy can be interrupted.
        Writes to a temporary "*.part" file and atomically replaces the destination on success.

        Raises:
            InterruptedError: if cancel_event is set during copy (caller should treat as canceled).
            Exception: on other IO errors.
        """
        temp_path = dst_path + ".part"
        # Ensure parent exists (caller often already created, but double-check)
        os.makedirs(os.path.dirname(dst_path), exist_ok=True)
        try:
            with open(src_path, "rb") as r, open(temp_path, "wb") as w:
                while True:
                    if cancel_event.is_set():
                        # Clean up temp and notify caller
                        try:
                            w.close()
                        except Exception:
                            pass
                        try:
                            os.remove(temp_path)
                        except Exception:
                            pass
                        raise InterruptedError("copy cancelled")

                    # Measure read and write durations to detect stalls/very slow IO
                    t0 = time.time()
                    chunk = r.read(chunk_size)
                    t1 = time.time()
                    if (t1 - t0) > stall_timeout:
                        # Consider this a stall / very slow read and abort so caller can skip
                        try:
                            w.close()
                        except Exception:
                            pass
                        try:
                            os.remove(temp_path)
                        except Exception:
                            pass
                        raise Exception(f"stall: read took {t1-t0:.1f}s")

                    if not chunk:
                        break

                    t0w = time.time()
                    w.write(chunk)
                    t1w = time.time()
                    if (t1w - t0w) > stall_timeout:
                        try:
                            w.close()
                        except Exception:
                            pass
                        try:
                            os.remove(temp_path)
                        except Exception:
                            pass
                        raise Exception(f"stall: write took {t1w-t0w:.1f}s")
            # Try to copy file metadata (timestamps, perms) to the temp file
            try:
                shutil.copystat(src_path, temp_path)
            except Exception:
                # Non-fatal
                pass
            # Atomic replace
            os.replace(temp_path, dst_path)
        except InterruptedError:
            # propagate
            raise
        except Exception:
            # On error, remove temp if exists and re-raise
            try:
                if os.path.exists(temp_path):
                    os.remove(temp_path)
            except Exception:
                pass
            raise

    # ---------------- FTP helpers ----------------
    def _parse_ftp_url(self, url: str) -> dict:
        """Parse ftp:// URL. Returns dict with host, port, user, password, base_path."""
        p = urllib.parse.urlparse(url)
        if p.scheme != 'ftp':
            raise ValueError("Not an FTP URL")
        user = p.username or ""
        password = p.password or ""
        host = p.hostname
        port = p.port or 21
        # base path on server (strip leading '/')
        base = p.path or '/'
        if base.startswith('/'):
            base = base[1:]
        return {"host": host, "port": port, "user": user, "password": password, "base": base}

    def _ensure_ftp_dirs(self, ftp: ftplib.FTP, remote_dir: str) -> None:
        """Ensure directory tree exists on remote FTP server. remote_dir is posix-style without leading '/'."""
        # Walk through components and mkd if necessary
        if not remote_dir:
            return
        parts = [p for p in remote_dir.split('/') if p]
        cur = ""
        for part in parts:
            cur = f"{cur}/{part}" if cur else part
            try:
                ftp.mkd(cur)
            except ftplib.error_perm as e:
                # 550 indicates directory exists or not permitted; ignore exists
                # If it's a different error, re-raise
                err = str(e)
                if not (err.startswith('550') or 'File exists' in err):
                    # try to continue gracefully
                    pass

    class _FileWithCancel:
        """File-like wrapper that checks cancel_event on each read and measures read time for stall detection."""
        def __init__(self, path, cancel_event, chunk_size=64*1024, stall_timeout=60.0):
            self._f = open(path, 'rb')
            self.cancel_event = cancel_event
            self.chunk_size = chunk_size
            self.stall_timeout = stall_timeout

        def read(self, size=-1):
            if self.cancel_event.is_set():
                raise InterruptedError("upload cancelled")
            size = size if size and size > 0 else self.chunk_size
            t0 = time.time()
            data = self._f.read(size)
            t1 = time.time()
            if (t1 - t0) > self.stall_timeout:
                raise Exception(f"stall: read took {t1-t0:.1f}s")
            return data

        def close(self):
            try:
                self._f.close()
            except Exception:
                pass

    def _upload_file_to_ftp(self, src_path: str, remote_path: str, ftp_info: dict, cancel_event: threading.Event, chunk_size: int = 64*1024, stall_timeout: float = 60.0) -> None:
        """Upload a local file to remote FTP server at remote_path (posix-style). Uses a temporary .part upload then rename.

        Raises InterruptedError on cancel, Exception on other errors.
        """
        # remote_path like 'base/dir/file.ext' or 'dir/file.ext'
        # Separate dir and filename
        remote_dir, filename = (os.path.split(remote_path.replace('\\', '/'))) if '/' in remote_path else ('', remote_path)
        # normalize
        remote_dir = remote_dir.replace('\\', '/')
        if remote_dir.startswith('/'):
            remote_dir = remote_dir[1:]

        # Connect
        ftp = ftplib.FTP()
        try:
            ftp.connect(ftp_info['host'], ftp_info['port'], timeout=30)
            if ftp_info.get('user'):
                ftp.login(ftp_info.get('user'), ftp_info.get('password'))
            else:
                ftp.login()
            # Ensure directory tree exists
            if ftp_info.get('base'):
                # change to base first
                try:
                    ftp.cwd(ftp_info.get('base'))
                except Exception:
                    # try to create base
                    self._ensure_ftp_dirs(ftp, ftp_info.get('base'))
                    try:
                        ftp.cwd(ftp_info.get('base'))
                    except Exception:
                        pass
            # ensure remote subdirs
            if remote_dir:
                self._ensure_ftp_dirs(ftp, remote_dir)

            # Build full remote destination (relative to current cwd on server)
            full_remote = (remote_dir + '/' + filename) if remote_dir else filename
            # temporary name
            tmp_name = full_remote + '.part'

            # If file exists and overwrite not allowed, let caller manage. Here we always upload.
            # Use FileWithCancel so reads can be interrupted/detected for stalls
            fwrap = self._FileWithCancel(src_path, cancel_event, chunk_size=chunk_size, stall_timeout=stall_timeout)
            try:
                # storbinary will call fwrap.read() repeatedly
                ftp.storbinary(f'STOR {tmp_name}', fwrap, blocksize=chunk_size)
            finally:
                try:
                    fwrap.close()
                except Exception:
                    pass

            # rename temp -> final
            try:
                ftp.rename(tmp_name, full_remote)
            except Exception:
                # some servers may not allow rename across dirs; try delete final then rename
                try:
                    ftp.delete(full_remote)
                except Exception:
                    pass
                try:
                    ftp.rename(tmp_name, full_remote)
                except Exception:
                    # If rename fails, try storbinary directly to final (best-effort)
                    with open(src_path, 'rb') as f:
                        ftp.storbinary(f'STOR {full_remote}', f, blocksize=chunk_size)

        except InterruptedError:
            try:
                ftp.close()
            except Exception:
                pass
            raise
        except Exception:
            try:
                ftp.close()
            except Exception:
                pass
            raise
        finally:
            try:
                ftp.quit()
            except Exception:
                try:
                    ftp.close()
                except Exception:
                    pass
        


if __name__ == "__main__":
    app = ImageCopyApp()
    app.mainloop()
