/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 *
 * @providesModule DatePickerWindows
 * @flow
 */
'use strict';

var React = require('React');
var View = require('View');
var requireNativeComponent = require('requireNativeComponent');

var ReactNative = require("react-native");

var DatePickerWindows = React.createClass({
    name: 'DatePickerWindows',
    propTypes: {
        ...View.PropTypes,
        ...ReactNative.ViewPropTypes,

        /**
         * The currently selected date.
         */
        date: React.PropTypes.instanceOf(Date),

        /**
         * Date change handler.
         *
         * This is called when the user changes the date or time in the UI.
         * The first and only argument is a Date object representing the new
         * date and time.
         */
        onChange: React.PropTypes.func,

        /**
         * Maximum year.
         *
         * Restricts the range with an upper bound on a date.
         */
        maxDate: React.PropTypes.instanceOf(Date),

        /**
         * Minimum year.
         *
         * Restricts the range with a lower bound on a date.
         */
        minDate: React.PropTypes.instanceOf(Date),
    },
    getDefaultProps: function() {
      return {
          date: new Date(),
      };
    },
    _onChange: function(event) {
      this.props.onChange && this.props.onChange(new Date(event.nativeEvent.date));
    },
    render: function(){
        return <RCTDatePicker date={this.props.date}
                    minDate={this.props.minDate}
                    maxDate={this.props.maxDate}
                    onChange={this._onChange} />
    },
});

var RCTDatePicker = requireNativeComponent('RCTDatePicker', DatePickerWindows);

module.exports = DatePickerWindows;