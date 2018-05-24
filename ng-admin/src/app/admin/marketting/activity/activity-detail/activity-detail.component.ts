import { Component, OnInit, Injector, EventEmitter, AfterViewInit, OnDestroy, Input, Output } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ArticleServiceProxy } from '@shared/service-proxies/marketing-service';
import { Article } from '@shared/entity/marketting';
import { ResourceLoader } from '@angular/compiler';
import { NzModalService, UploadFile } from 'ng-zorro-antd';
import { AppConsts } from '@shared/AppConsts';

@Component({
    moduleId: module.id,
    selector: 'activity-detail',
    templateUrl: 'activity-detail.component.html',
    styleUrls: ['activity-detail.component.scss']
})
export class ActivityDetailComponent extends AppComponentBase implements OnInit {
    form: FormGroup;
    id: number;
    article: Article = new Article();
    isConfirmLoading = false;
    //用于按钮是否显示
    isPush = true;
    isDelete = false;
    successMsg = '';
    cardTitle = '';
    host = AppConsts.remoteServiceBaseUrl;
    actionUrl = this.host + '/WeChatFile/MarketingInfoPosts?fileName=activity';

    config_classic: any = {
        height: 550,

        plugins: [
            'advlist autolink autosave link image lists charmap print preview hr anchor pagebreak',
            'searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking',
            'table contextmenu directionality emoticons template textcolor paste fullpage textcolor colorpicker textpattern'
        ],
        toolbar1: 'bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | styleselect formatselect fontselect fontsizeselect',
        // toolbar1: 'newdocument fullpage | bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | styleselect formatselect fontselect fontsizeselect',
        toolbar2: 'bullist numlist | outdent indent blockquote | undo redo | image code| insertdatetime preview | forecolor backcolor',
        // toolbar2: 'cut copy paste | searchreplace | bullist numlist | outdent indent blockquote | undo redo | link unlink anchor image media code | insertdatetime preview | forecolor backcolor',
        // toolbar3: 'table | hr removeformat | subscript superscript | charmap emoticons | print fullscreen | ltr rtl | visualchars visualblocks nonbreaking template pagebreak restoredraft',
        toolbar3: 'table | hr removeformat | charmap | ltr rtl | visualchars visualblocks template pagebreak restoredraft',
        // content_css: [
        //     '//fonts.googleapis.com/css?family=Lato:300,300i,400,400i',
        //     '//www.tinymce.com/css/codepen.min.css'],
        language: 'zh_CN',
        menubar: false,
        toolbar_items_size: 'small',
        init_instance_callback: function () {
        },
        images_upload_url: this.actionUrl,
        images_upload_handler: function (blobInfo, success, failure) {
            setTimeout(function() {
              // no matter what you upload, we will turn it into TinyMCE logo :)
              success('http://moxiecode.cachefly.net/tinymce/v9/images/logo.png');
            }, 2000);
          }
    };
    constructor(injector: Injector, private fb: FormBuilder, private actRouter: ActivatedRoute,
        private activityService: ArticleServiceProxy, private router: Router, private modal: NzModalService) {
        super(injector);
        this.id = this.actRouter.snapshot.params['id'];
    }

    // set(key: string, value: any) {
    //     const obj: any = {};
    //     obj[key] = value;
    //     this.config = Object.assign({}, this.config, obj);
    // }
    ngOnInit(): void {
        this.form = this.fb.group({
            title: [null, Validators.compose([Validators.required, Validators.maxLength(200)])],
            author: [null, Validators.compose([Validators.required, Validators.maxLength(50)])],
            // coverPhoto: [null, Validators.compose([Validators.required])], 图片能上传时
            coverPhoto: [null],
            content: [null],
        });
        this.getSingleActivity();
    }

    getSingleActivity() {
        if (this.id) {
            this.activityService.get(this.id).subscribe((result: Article) => {
                this.article = result;
                this.isDelete = true;
                this.isPush = result.pushStatus === 1 ? false : true;
                this.cardTitle = '编辑活动';
                if (result.coverPhoto) {
                    this.article.showCoverPhoto = this.host + this.article.coverPhoto;
                }
            });
        } else {
            //新增
            this.article.pushStatus = 0;
            this.article.pushStatusName = '草稿';
            this.article.type = 1;//类型为活动
            this.cardTitle = '新增活动';
        }
    }

    getFormControl(name: string) {
        return this.form.controls[name];
    }
    saveActivity() {
        this.activityService.update(this.article)
            .finally(() => { this.isConfirmLoading = false; })
            .subscribe((result: Article) => {
                this.article = result;
                if (result.coverPhoto) {
                    this.article.showCoverPhoto = this.host + this.article.coverPhoto;
                }
                this.isDelete = true;
                this.isPush = result.pushStatus === 1 ? false : true;
                this.notify.info(this.l(this.successMsg));
                this.cardTitle = '编辑活动';
            });
    }


    save(isPulish = false) {
        for (const i in this.form.controls) {
            this.form.controls[i].markAsDirty();
        }
        if (this.form.valid) {
            this.isConfirmLoading = true;
            this.successMsg = isPulish === false ? '保存成功！' : '发布成功！';
            this.saveActivity();
        }
    }
    push() {
        //发布
        this.article.pushStatus = 1;
        this.article.pushTime = this.dateFormat(new Date());
        this.save(true);
    }
    delete(TplContent) {
        this.modal.confirm({
            content: TplContent,
            cancelText: '否',
            okText: '是',
            onOk: () => {
                this.activityService.delete(this.article.id).subscribe(() => {
                    this.notify.info(this.l('删除成功！'));
                    this.return();
                });
            }
        })
    }
    return() {
        this.router.navigate(['admin/marketting/activity']);
    }

    private getBase64(img: File, callback: (img: any) => void) {
        const reader = new FileReader();
        reader.addEventListener('load', () => callback(reader.result));
        reader.readAsDataURL(img);
    }

    //图片上传返回
    handleChange(info: { file: UploadFile }): void {
        console.table(info);

        if (info.file.status === 'error') {
            this.notify.error('上传图片异常，请重试');
        }
        if (info.file.status === 'done') {
            this.getBase64(info.file.originFileObj, (img: any) => {
                this.article.showCoverPhoto = img;
            });
            this.article.coverPhoto = info.file.response.result.imageName;
            this.notify.success('上传图片完成');
        }
    }
}
