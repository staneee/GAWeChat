import { Component, OnInit, Output, EventEmitter, Injector } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { EmployeeServiceProxy } from '@shared/service-proxies/marketing-service';
import { NzModalService } from 'ng-zorro-antd';
import { Employee } from '@shared/entity/marketting';

@Component({
    moduleId: module.id,
    selector: 'create-employee-modal',
    templateUrl: 'create-employee.component.html',
})
export class CreateEmployeeComponent extends AppComponentBase implements OnInit{
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();

    cmodalVisible = false;
    cloading = false;
    isConfirmLoading = false;
    employeec: Employee = new Employee();
    formc: FormGroup;
    positions = [
        { text: '营销人员', value: 1 },
        { text: '客户经理', value: 2 },
        { text: '营销中心', value: 3 },
        { text: '机关人员', value: 4 },
    ]
    isCodeCValid: boolean=false;
    constructor(injector: Injector, private fb: FormBuilder, private employeeService: EmployeeServiceProxy,
        private modal: NzModalService) {
        super(injector);
    }

    /**
     * 页面初始加载
     */
    ngOnInit(): void {
        this.formc = this.fb.group({
            code: [null, Validators.compose([Validators.required,Validators.minLength(6), Validators.maxLength(50),Validators.pattern('^[0-9]*$')])],
            name: [null, Validators.compose([Validators.required, Validators.maxLength(50)])],
            position: [null, [Validators.required]],
            phone: [null, Validators.compose([Validators.pattern('^1[3|4|5|7|8|9][0-9]{9}$')])],
            company: [null, Validators.compose([Validators.maxLength(200)])],
            department: [null, Validators.compose([Validators.maxLength(200)])],
            isactive: [true],
        })
    }

    /**
     * 显示模态框（进入新增页）
     */
    show() {
        this.reset();
        this.employeec = new Employee();
        // this.employeec.position = 2;
        this.employeec.init({ isAction: true, position: 2 })
        this.cmodalVisible = true;

    }
    getFormControl(name: string) {
        return this.formc.controls[name];
    }
    /**
     * 取消按钮事件
     */
    chandleCancel = (e) => {
        this.cmodalVisible = false;
        this.isConfirmLoading = false;
        this.reset(e);
    }
    savec(deleteContent) {

        //检查form验证
        for (const i in this.formc.controls) {
            this.formc.controls[i].markAsDirty();
        }
        if (this.formc.valid) {
            this.isConfirmLoading = true;
            this.employeeService.CheckCode(this.employeec.code).subscribe((isCode: boolean) => {
                if (isCode) {
                    this.employeeService.update(this.employeec)
                        .finally(() => { this.isConfirmLoading = false; })
                        .subscribe(() => {
                            this.notify.info(this.l('保存成功！'));
                            this.cmodalVisible = false;
                            this.modalSave.emit(null);
                        });
                } else {
                    this.isConfirmLoading = false;
                    // this.isCodeCValid=true;
                    this.notify.error('员工编码已存在');
                }
            });

        }

    }
    reset(e?): void {
        if (e) {
            e.preventDefault();
        }
        this.formc.reset();
        for (const key in this.formc.controls) {
            this.formc.controls[key].markAsPristine();
        }
    }

   
}
