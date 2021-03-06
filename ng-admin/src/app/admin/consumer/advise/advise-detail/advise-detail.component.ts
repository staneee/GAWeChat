import { Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { Advise } from '@shared/entity/consumer';
import { AdviseService } from '@shared/service-proxies/consumer-service';
import { AppConsts } from '@shared/AppConsts';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    moduleId: module.id,
    selector: 'advise-detail',
    templateUrl: 'advise-detail.component.html',
})
export class AdviseDetailComponent extends AppComponentBase implements OnInit{
    id = '';
        advise: Advise = new Advise();
        photoUrls: string[];
        //图片放大
        previewImage = '';
        previewVisible = false;
        //图片路径前缀
        host = '';
        constructor(injector: Injector, private adviseService: AdviseService, private actRouter: ActivatedRoute, private router: Router, ) {
            super(injector);
           this.id = this.actRouter.snapshot.params['id'];
        }
    
        ngOnInit(): void {
            this.getSingleAdvise();
            this.host = AppConsts.remoteServiceBaseUrl;
        }
    
        getSingleAdvise() {
            this.adviseService.get(this.id).subscribe((result: Advise) => {
                this.advise = result;
                this.photoUrls = result.getPhotoUrls();
            });
       }
    //图片放大
    handlePreview = (url: string) => {
            this.previewImage = url;
            this.previewVisible = true;
        }
    
       Return() {
            this.router.navigate(['admin/consumer/advise']);
        }
}
