import { IConfig } from 'umi-types';

// ref: https://umijs.org/config/
const config: IConfig = {
  treeShaking: true,
  plugins: [
    // ref: https://umijs.org/plugin/umi-plugin-react.html
    [
      'umi-plugin-react',
      {
        antd: true,
        dva: true,
        dynamicImport: false,
        title: 'UI2',
        dll: false,

        routes: {
          exclude: [
            /models\//,
            /services\//,
            /model\.(t|j)sx?$/,
            /service\.(t|j)sx?$/,
            /components\//,
          ],
        },
      },
    ],
  ],
  routes: [
    { path: '/', component: './login' },
    {
      path: '/case-list',
      component: '../layouts/index',
      routes: [
        { path: '/case-list/:id', component: './case-list' },
        { path: '/case-list', component: './case-list' },
      ],
    },
  ],
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      secure: false,
    },
    '/JWTAuth': {
      target: 'http://localhost:5000',
      secure: false,
    },
  },
};

export default config;
