import React from 'react';
import styles from './login.css';
import { Form, Input, Icon, Checkbox, Button } from 'antd';
import Link from 'umi/link';

export default class LoginForm extends React.Component {
  handleSubmit = (e: any) => {
    e.preventDefault();
    //   this.props.form.validateFields((err, values) => {
    //     if (!err) {
    //       console.log('Received values of form: ', values);
    //     }
    //   });
  };

  render() {
    return (
      <Form onSubmit={this.handleSubmit} className={styles.loginForm}>
        <Form.Item>
          <Input
            prefix={<Icon type="user" style={{ color: 'rgba(0,0,0,.25)' }} />}
            placeholder="Username"
          />
        </Form.Item>
        <Form.Item>
          <Input
            prefix={<Icon type="lock" style={{ color: 'rgba(0,0,0,.25)' }} />}
            type="password"
            placeholder="Password"
          />
        </Form.Item>
        <Form.Item>
          <Checkbox>Remember me</Checkbox>
          <a className={styles.loginFormForgot} href="">
            Forgot password
          </a>
          <Link to="/JWTAuth/Backdoor">
            <Button type="primary" htmlType="submit" className={styles.loginFormButton}>
              Log in
            </Button>
          </Link>
          Or <a href="">register now!</a>
        </Form.Item>
      </Form>
    );
  }
}