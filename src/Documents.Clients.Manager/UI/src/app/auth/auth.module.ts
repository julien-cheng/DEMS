import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AuthRoutingModule } from './auth-routing.module';
import { SharedModule } from '../shared/shared.module';
import { LoginComponent, EdiscoveryLandingComponent, LeoLandingComponent } from './index';

@NgModule({
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AuthRoutingModule, SharedModule],
  declarations: [LoginComponent, EdiscoveryLandingComponent, LeoLandingComponent]
})
export class AuthModule {}
