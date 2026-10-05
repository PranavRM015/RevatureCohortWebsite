import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { ApiService } from './api.service';
import { ColorPicker } from './color-picker/color-picker';
import { Home } from './home/home';
import { Profiles } from './profiles/profiles';
import { Shell } from './shell/shell';
import { Signin } from './signin/signin';
import { TopicPage } from './topic/topic';

const requireLogin: CanActivateFn = async () => {
  const api = inject(ApiService);
  const router = inject(Router);
  return (await api.loadMe()) ? true : router.parseUrl('/signin');
};

const requireColor: CanActivateFn = async () => {
  const api = inject(ApiService);
  const router = inject(Router);
  const me = await api.loadMe();
  if (!me) return router.parseUrl('/signin');
  return me.profileColor.toUpperCase() === '#000000' ? router.parseUrl('/color') : true;
};

export const routes: Routes = [
  { path: 'signin', component: Signin },
  { path: 'color', component: ColorPicker, canActivate: [requireLogin] },
  {
    path: '',
    component: Shell,
    canActivate: [requireColor],
    children: [
      { path: '', component: Home },
      { path: 'topics/:slug', component: TopicPage },
      { path: 'people', component: Profiles },
    ],
  },
  { path: '**', redirectTo: '' },
];
