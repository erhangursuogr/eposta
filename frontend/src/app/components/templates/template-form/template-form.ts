import { Component, inject, OnInit, AfterViewInit, OnDestroy, signal, ViewChild, ElementRef, ViewEncapsulation } from '@angular/core';

import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { TemplateService } from '../../../services/template.service';
import { TemplateCategoryService } from '../../../services/template-category.service';
import { TemplateCategory } from '../../../common/models/template-category.model';
import { CategoryManagementDialogComponent } from '../category-management-dialog/category-management-dialog';
import suneditor from 'suneditor';
import tr from 'suneditor/src/lang/tr';
import plugins from 'suneditor/src/plugins';
import { TemplatePreview } from '../../../common/models/template.model';

@Component({
  selector: 'app-template-form',
  standalone: true,
  imports: [
    RouterModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatSelectModule,
    MatTooltipModule,
    MatDialogModule
],
  templateUrl: './template-form.html',
  styleUrl: './template-form.css',
  encapsulation: ViewEncapsulation.None
})
export class TemplateFormComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly templateService = inject(TemplateService);
  private readonly categoryService = inject(TemplateCategoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly sanitizer = inject(DomSanitizer);

  @ViewChild('suneditor', { static: false }) editorElement!: ElementRef;
  private editor: any;

  templateForm: FormGroup;
  loading = signal(false);
  isEditMode = signal(false);
  templateId: number | null = null;
  preview = signal<TemplatePreview | null>(null);
  loadingPreview = signal(false);
  categories = signal<TemplateCategory[]>([]);

  // Template variable hints
  readonly variableHint = 'Değişkenler: {{konu}}, {{tarih}}, {{gonderen}}';
  readonly contentVariableHint = 'HTML içerik oluşturun. Değişkenler: {{konu}}, {{icerik}}, {{tarih}}, {{gonderen}}';

  constructor() {
    this.templateForm = this.fb.group({
      ad: ['', Validators.required],
      konu: [''],
      icerik: ['', Validators.required],
      kategoriId: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.templateId = parseInt(id);
      this.loadTemplate(this.templateId);
    }
  }

  loadCategories(): void {
    this.categoryService.getActiveCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories.set(response.data);
        }
      },
      error: () => {
        // Sessizce hata yönet
      }
    });
  }

  ngAfterViewInit(): void {
    if (this.editorElement) {
      this.editor = suneditor.create(this.editorElement.nativeElement, {
        lang: tr,
        plugins: plugins,
        placeholder: 'Yazı tipi Times New Roman, boyut 12pt, olarak ayarlanmıştır. Lütfen içeriğinizi buraya yapıştırın veya yazın.',
        popupDisplay: 'local',
        font: ['Arial', 'Calibri', 'Comic Sans MS', 'Courier New', 'Georgia', 'Impact', 'Tahoma', 'Times New Roman', 'Verdana'],
        minHeight: '40rem',
        height: '700',
        width: '100%',
        buttonList: [
          ['undo', 'redo'],
          ['font', 'fontSize', 'formatBlock'],
          ['bold', 'underline', 'italic', 'strike', 'subscript', 'superscript','paragraphStyle','blockquote','textStyle'],
          ['fontColor', 'hiliteColor'],
          ['removeFormat'],
          ['outdent', 'indent'],
          ['align', 'horizontalRule', 'list', 'table'],
          ['link', 'image', 'video'],
          ['fullScreen', 'showBlocks', 'codeView'],
          ['preview', 'print']
        ],
        imageResizing: true,
        imageHeightShow: false,
        imageFileInput: true,
        imageUploadSizeLimit: 2 * 1024 * 1024, // 2MB per image (base64 embed)
        imageUrlInput: true, // URL girişi etkinleştir
        videoFileInput: false,
        callBackSave: (contents: string) => {
          this.templateForm.patchValue({ icerik: contents }, { emitEvent: false });
        },
        pasteTagsWhitelist: 'p|h1|h2|h3|h4|h5|h6|ul|ol|li|strong|em|u|s|a|img|table|thead|tbody|tr|th|td|br|span|div',
        attributesWhitelist: {
          // GÜVENLİK: 'all: style' yerine sadece gerekli taglarda style izni
          p: 'style',
          div: 'style',
          span: 'style',
          table: 'cellpadding|cellspacing|border|style',
          td: 'style',
          th: 'style',
          img: 'src|alt|style|width|height',
          a: 'href|target|rel'
        },
        // Word paste config
        addTagsWhitelist: 'p|div|pre|blockquote|h1|h2|h3|h4|h5|h6|ol|ul|li|hr|figure|figcaption|img|iframe|audio|video|table|thead|tbody|tr|th|td|a|b|strong|var|i|em|u|ins|s|span|strike|del|sub|sup|code|svg|path',
        pasteTagsBlacklist: '',
        imageAccept: '.jpg,.jpeg,.png,.gif,.bmp,.webp',
        defaultStyle: 'font-family: Times New Roman; font-size: 12pt; line-height: 1.5;'
      });

      this.editor.onChange = (contents: string) => {
        this.templateForm.patchValue({ icerik: contents }, { emitEvent: false });
      };

      // Load existing content if editing
      if (this.isEditMode() && this.templateForm.value.icerik) {
        this.editor.setContents(this.templateForm.value.icerik);
      }
    }
  }

  ngOnDestroy(): void {
    if (this.editor) {
      this.editor.destroy();
    }
  }

  loadTemplate(id: number): void {
    this.loading.set(true);
    this.templateService.getTemplateById(id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templateForm.patchValue({
            ad: response.data.sablonAdi,
            konu: response.data.konuSablonu,
            icerik: response.data.icerikSablonu,
            kategoriId: response.data.kategoriId
          });

          // Set editor content after form is patched
          if (this.editor && response.data) {
            setTimeout(() => {
              this.editor.setContents(response.data!.icerikSablonu);
            }, 100);
          }
        }
        this.loading.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Şablon yüklenemedi';
        this.showMessage(errorMessage, 'error');
        this.loading.set(false);
      }
    });
  }

  loadPreview(): void {
    if (!this.templateId) {
      this.showMessage('Önizleme için önce şablonu kaydedin', 'error');
      return;
    }

    this.loadingPreview.set(true);
    this.templateService.previewTemplate(this.templateId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.preview.set(response.data);
        } else {
          this.showMessage(response.message || 'Önizleme yüklenemedi', 'error');
        }
        this.loadingPreview.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Önizleme yüklenirken hata oluştu';
        this.showMessage(errorMessage, 'error');
        this.loadingPreview.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.templateForm.invalid) return;

    this.loading.set(true);
    const data = this.templateForm.value;

    const request$ = this.isEditMode()
      ? this.templateService.updateTemplate(this.templateId!, data)
      : this.templateService.createTemplate(data);

    request$.subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage(
            this.isEditMode() ? 'Şablon güncellendi' : 'Şablon oluşturuldu',
            'success'
          );
          this.router.navigate(['/sablonlar']);
        } else {
          this.showMessage(response.message || 'İşlem başarısız', 'error');
        }
        this.loading.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'İşlem sırasında hata oluştu';
        this.showMessage(errorMessage, 'error');
        this.loading.set(false);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/sablonlar']);
  }

  openCategoryManagement(): void {
    const dialogRef = this.dialog.open(CategoryManagementDialogComponent, {
      width: '700px',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Kategoriler güncellenmiş olabilir, listeyi yenile
        this.loadCategories();
      }
    });
  }

  private showMessage(message: string, type: 'success' | 'error'): void {
    this.snackBar.open(message, 'Kapat', {
      duration: 3000,
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error'
    });
  }

  getSafeHtml(html: string): SafeHtml {
    // Angular built-in XSS protection - sanitize metodu otomatik HTML context kullanır
    return this.sanitizer.sanitize(1, html) || '';
  }
}
